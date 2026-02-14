using System.Text.Json;
using System.IO;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Protocol;
using Codexus.Cipher.Utils;
using Codexus.Cipher.Utils.Http;
using Codexus.Development.SDK.Entities;
using Codexus.Development.SDK.Manager;
using Codexus.Development.SDK.Utils;
using Codexus.Game.Launcher;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using HydPro.Launcher;
using HydPro.Launcher.Entities;
using Serilog;
ConfigureLogger();
Log.Information("===========================================");
Log.Information("* 基于 OpenSDK.NEL , Codexus.OpenSDK 以及 Codexus.Development.SDK");
Log.Information("===========================================");
Log.Information("初始化系统组件");
await InitializeSystemComponentsAsync();
Log.Information("系统组件初始化完成");
Log.Information("创建服务实例");
var (services, crcSalt, gameVersion) = await CreateServices();
Log.Information("服务实例创建完成");
Log.Information("启动TCP服务器");
var tcpServer = new TcpServer();
_ = Task.Run(async () => await tcpServer.StartAsync());
Log.Information("TCP服务器启动完成");

await Task.Delay(Timeout.Infinite);
static void ConfigureLogger()
{
    var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
    Directory.CreateDirectory(logsDir);
    var logFileName = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log";
    var logFilePath = Path.Combine(logsDir, logFileName);
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Filter.ByExcluding(logEvent => logEvent.RenderMessage().Contains("Unknown DataComponent type:"))
        .WriteTo.Console()
        .WriteTo.File(logFilePath)
        .CreateLogger();
}
static async Task InitializeSystemComponentsAsync()
{
    Interceptor.EnsureLoaded();
    PacketManager.Instance.EnsureRegistered();
    PluginManager.Instance.EnsureUninstall();
    PluginManager.Instance.LoadPlugins("plugins");
    await Task.CompletedTask;
}
static async Task<(Services, string, string)> CreateServices()
{
    var api = new WebNexusApi("YXBwSWQ9Q29kZXh1cy5HYXRld2F5LmFwcFNlY3JldD1hN0s5bTJYcUw4YkM0d1ox");
    var wpf = new WPFLauncher();
    var (crcSalt, gameVersion) = await ComputeCrcSalt();
    Log.Information("使用 CRC Salt: {Salt} 和 Game Version: {Version}", crcSalt, gameVersion);
    return (new Services(api, wpf), crcSalt, gameVersion);
}
static async Task<(string, string)> ComputeCrcSalt()
{
    Log.Information("正在计算 CRC Salt 和 Game Version...");
    const string token = "0e9327a2-d0f8-41d5-8e23-233de1824b9a.pk_053ff2d53503434bb42fe158";
    try
    {
        var http = new HttpWrapper("https://service.codexus.today");
        var response = await http.GetAsync("/crc-salt", builder =>
        {
            builder.AddHeader("Authorization", $"Bearer {token}");
        });
        Log.Information("响应状态码: {StatusCode}", response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Log.Information("响应内容: {Content}", json);
        var entity = JsonSerializer.Deserialize<HydPro.Launcher.Entities.OpenSdkResponse<HydPro.Launcher.Entities.CrcSalt>>(json);
        if (entity != null && entity.Success && entity.Data != null)
        {
            Log.Information("成功获取 CRC Salt: {Salt} 和 Game Version: {Version}", entity.Data.Salt, entity.Data.Version);
            return (entity.Data.Salt, entity.Data.Version);
        }
        else
        {
            Log.Error("响应解析失败: {Json}", json);
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "获取 CRC Salt 和 Game Version 时发生异常");
    }
    Log.Error("无法计算出 CrcSalt 和 Game Version");
    return (string.Empty, string.Empty);
}
internal record Services(
    WebNexusApi Api,
    WPFLauncher Wpf
);