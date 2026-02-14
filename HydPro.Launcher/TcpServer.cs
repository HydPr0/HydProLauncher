using System.Net;
using System.Net.Sockets;
using System.Text;
using Serilog;
namespace HydPro.Launcher;
public class TcpServer
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CommandHandler _commandHandler;
    private const int DefaultPort = 3000;
    private const int TimeoutSeconds = 10;
    public TcpServer(int port = DefaultPort)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _cancellationTokenSource = new CancellationTokenSource();
        _commandHandler = new CommandHandler();
    }
    public async Task StartAsync()
    {
        _listener.Start();
        Log.Information("TCP已启动，监听端口: {Port}", DefaultPort);
        try
        {
            await AcceptConnectionsAsync(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Log.Information("TCP正在停止...");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "TCP发生错误");
        }
        finally
        {
            _listener.Stop();
            Log.Information("TCP已停止");
        }
    }
    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                break;
            }
        }
    }
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var remoteEndPoint = client.Client.RemoteEndPoint;
        Log.Information("客户端已连接: {RemoteEndPoint}", remoteEndPoint);
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                client.ReceiveTimeout = TimeoutSeconds * 1000;
                client.SendTimeout = TimeoutSeconds * 1000;
                var buffer = new byte[4096];
                var readBuffer = new StringBuilder();
                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    readBuffer.Append(receivedData);
                    var data = readBuffer.ToString();
                    var newlineIndex = data.IndexOf('\n');
                    while (newlineIndex >= 0)
                    {
                        var command = data.Substring(0, newlineIndex).Trim();
                        if (!string.IsNullOrEmpty(command))
                        {
                            Log.Information("收到命令: {Command}", command);
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var response = await _commandHandler.HandleCommandAsync(command);
                                    var responseData = Encoding.UTF8.GetBytes($"{response}\n");
                                    lock (stream)
                                    {
                                        if (client.Connected)
                                        {
                                            stream.Write(responseData, 0, responseData.Length);
                                            stream.Flush();
                                        }
                                    }
                                    Log.Information("发送响应: {Response}", response);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "处理命令时发生错误: {Command}", command);
                                }
                            }, cancellationToken);
                        }
                        data = data.Substring(newlineIndex + 1);
                        readBuffer.Clear();
                        readBuffer.Append(data);
                        newlineIndex = data.IndexOf('\n');
                    }
                }
            }
        }
        catch (IOException ex) when (ex.InnerException is SocketException)
        {
            Log.Information("客户端断开连接: {RemoteEndPoint}", remoteEndPoint);
        }
        catch (OperationCanceledException)
        {
            Log.Information("客户端连接已取消: {RemoteEndPoint}", remoteEndPoint);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "处理客户端连接时发生错误: {RemoteEndPoint}", remoteEndPoint);
        }
        finally
        {
            Log.Information("客户端连接已关闭: {RemoteEndPoint}", remoteEndPoint);
        }
    }
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _listener.Stop();
    }
}