using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Protocol;
using Codexus.Cipher.Utils.Http;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using Serilog;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
namespace HydPro.Launcher;
public class CommandHandler
{
    private const string ServerId = "4661334467366178884";
    private const string CrcSaltUrl = "https://service.codexus.today/crc-salt";
    private const string CrcSaltToken = "0e9327a2-d0f8-41d5-8e23-233de1824b9a.pk_053ff2d53503434bb42fe158";
    private readonly AccountManager _accountManager;
    private readonly WPFLauncher _wpfLauncher;
    private readonly HttpWrapper _httpClient;
    private readonly PlayerProxyManager _playerProxyManager;
    private static string _crcSalt = "B73962A7833192F9CAD0D68A2AA4462E";
    private static string _gameVersion = "1.15.18.46492";
    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();


    public CommandHandler()
    {
        _accountManager = new AccountManager();
        _wpfLauncher = new WPFLauncher();
        _httpClient = new HttpWrapper();
        _playerProxyManager = new PlayerProxyManager();
    }
    public async Task<string> HandleCommandAsync(string command)
    {
        await InitializeAsync();
        var result = CommandParser.Parse(command);
        if (!result.Success)
        {
            return $"{result.InviteCode}|{result.PlayerName}|{result.Error}";
        }
        try
        {
            return await ProcessCommandAsync(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "处理命令时发生错误: {Command}", result.Command);
            return $"{result.InviteCode}|{result.PlayerName}|处理命令失败: {ex.Message}";
        }
    }
    private static async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            lock (_initLock)
            {
                if (!_isInitialized)
                {
                    Log.Information("开始初始化 crcsalt 和 game 版本");
                    var initTask = Task.Run(async () =>
                    {
                        try
                        {
                            var http = new HttpWrapper();
                            var response = await http.GetAsync(CrcSaltUrl, builder =>
                            {
                                builder.AddHeader("Authorization", $"Bearer {CrcSaltToken}");
                            });
                            response.EnsureSuccessStatusCode();
                            var json = await response.Content.ReadAsStringAsync();
                            var entity = JsonSerializer.Deserialize<HydPro.Launcher.Entities.OpenSdkResponse<HydPro.Launcher.Entities.CrcSalt>>(json);
                            if (entity != null && entity.Success && entity.Data != null)
                            {
                                _crcSalt = entity.Data.Salt;
                                Log.Information("成功获取 CRC Salt: {Salt}", _crcSalt);
                            }
                            else
                            {
                                Log.Error("响应解析失败: {Json}", json);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "获取 CRC Salt 时发生异常，使用默认值");
                        }
                    });
                    initTask.Wait();
                    _isInitialized = true;
                    Log.Information("初始化完成");
                }
            }
        }
    }
    private async Task<string> ProcessCommandAsync(CommandResult result)
    {
        return result.Command switch
        {
            "caccount" => await HandleCAccountCommandAsync(result),
            "state" => await HandleStateCommandAsync(result),
            "create" => await HandleCreateCommandAsync(result),
            "join" => await HandleJoinCommandAsync(result),
            "relogin" => await HandleReloginCommandAsync(result),
            "token" => await HandleTokenLoginCommandAsync(result),
            "login" => await HandleLoginCommandAsync(result),

            _ => $"{result.InviteCode}|{result.PlayerName}|不支持的命令: {result.Command}"
        };
    }
    private async Task<string> HandleCAccountCommandAsync(CommandResult result)
    {
        try
        {
            return $"{result.InviteCode}|{result.PlayerName}|获取CAccount功能已被移除";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取CAccount失败");
            return $"{result.InviteCode}|{result.PlayerName}|获取CAccount失败: {ex.Message}";
        }
    }
    private async Task<string> HandleStateCommandAsync(CommandResult result)
    {
        var accountInfo = _accountManager.GetAccount(result.InviteCode);
        if (accountInfo == null)
        {
            return $"{result.InviteCode}|{result.PlayerName}|未登录，请使用CAccount获取";
        }
        try
        {
            var roles = _wpfLauncher.QueryNetGameCharacters(accountInfo.EntityId, accountInfo.Token, ServerId);
            if (roles.Data.Length == 0)
            {
                return $"{result.InviteCode}|{result.PlayerName}|无角色，请使用create创建";
            }
            var roleList = new StringBuilder("角色列表:");
            for (int i = 0; i < roles.Data.Length; i++)
            {
                if (i > 0)
                {
                    roleList.Append(",");
                }
                roleList.Append($"{i + 1}.{roles.Data[i].Name}");
            }
            return $"{result.InviteCode}|{result.PlayerName}|{roleList}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "查询角色列表失败");
            return $"{result.InviteCode}|{result.PlayerName}|查询角色列表失败: {ex.Message}";
        }
    }
    private async Task<string> HandleCreateCommandAsync(CommandResult result)
    {
        var accountInfo = _accountManager.GetAccount(result.InviteCode);
        if (accountInfo == null)
        {
            return $"{result.InviteCode}|{result.PlayerName}|未登录，请使用CAccount获取";
        }
        if (result.Parameters.Length == 0)
        {
            return $"{result.InviteCode}|{result.PlayerName}|参数格式错误，请提供角色名称";
        }
        var roleName = result.Parameters[0];
        try
        {
            _wpfLauncher.CreateCharacter(accountInfo.EntityId, accountInfo.Token, ServerId, roleName);
            Log.Information("创建角色成功: {RoleName}", roleName);
            return $"{result.InviteCode}|{result.PlayerName}|创建成功:{roleName}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建角色失败: {RoleName}", roleName);
            return $"{result.InviteCode}|{result.PlayerName}|创建失败:{ex.Message}";
        }
    }
    private async Task<string> HandleJoinCommandAsync(CommandResult result)
    {
        var remainingPoints = await CheckInviteCodePointsAsync(result.InviteCode);
        if (remainingPoints <= 0)
        {
            return $"{result.InviteCode}|{result.PlayerName}|邀请码点数不足无法Join";
        }
        var accountInfo = _accountManager.GetAccount(result.InviteCode);
        if (accountInfo == null)
        {
            return $"{result.InviteCode}|{result.PlayerName}|未登录，请使用CAccount获取";
        }
        if (result.Parameters.Length == 0)
        {
            return $"{result.InviteCode}|{result.PlayerName}|参数格式错误，请提供角色索引";
        }
        if (!int.TryParse(result.Parameters[0], out var roleIndex) || roleIndex < 1 || roleIndex > 3)
        {
            return $"{result.InviteCode}|{result.PlayerName}|角色索引错误，必须为1-3的数字";
        }
        try
        {
            var roles = _wpfLauncher.QueryNetGameCharacters(accountInfo.EntityId, accountInfo.Token, ServerId);
            if (roles.Data.Length < roleIndex)
            {
                return $"{result.InviteCode}|{result.PlayerName}|角色不存在";
            }
            var selectedCharacter = roles.Data[roleIndex - 1];
            var details = _wpfLauncher.QueryNetGameDetailById(accountInfo.EntityId, accountInfo.Token, ServerId);
            var address = _wpfLauncher.GetNetGameServerAddress(accountInfo.EntityId, accountInfo.Token, ServerId);
            var version = details.Data!.McVersionList[0];
            var gameVersionEnum = GameVersionUtil.GetEnumFromGameVersion(version.Name);
            var serverModInfo = await InstallerService.InstallGameMods(
                accountInfo.EntityId,
                accountInfo.Token,
                gameVersionEnum,
                _wpfLauncher,
                ServerId,
                false);
            var mods = JsonSerializer.Serialize(serverModInfo);
            var crcSalt = _crcSalt;
            var gameVersion = _gameVersion;
            Action<string> YggdrasilCallback = delegate (string serverId) {
                Log.Information("Server ID: {Certification}", serverId);
                var signal = new System.Threading.SemaphoreSlim(0);
                System.Threading.Tasks.Task.Run(async delegate {
                    try {
                        Log.Information("Minecraft 服务器认证回调收到");
                        var yggdrasil = new StandardYggdrasil(new YggdrasilData
                        {
                            LauncherVersion = gameVersion,
                            Channel = "netease",
                            CrcSalt = crcSalt
                        });
                        var pair = HydPro.Launcher.Utils.Md5Mapping.GetMd5FromGameVersion(version.Name);
                        var success = await yggdrasil.JoinServerAsync(new GameProfile
                        {
                            GameId = ServerId,
                            GameVersion = version.Name,
                            BootstrapMd5 = pair.BootstrapMd5,
                            DatFileMd5 = pair.DatFileMd5,
                            Mods = System.Text.Json.JsonSerializer.Deserialize<ModList>(mods)!,
                            User = new UserProfile { UserId = int.Parse(accountInfo.EntityId), UserToken = accountInfo.Token }
                        }, serverId);
                        if (success.IsSuccess)
                        {
                            Log.Information("消息认证成功");
                        }
                        else
                        {
                            Log.Error("消息认证失败: {Error}", success.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "认证过程中发生异常");
                    }
                    finally
                    {
                        signal.Release();
                    }
                });
                signal.Wait();
            };
            Log.Information("角色 {RoleName} 直接连接", selectedCharacter.Name);
            await DeductInviteCodePointsAsync(result.InviteCode, 1);
            return $"{result.InviteCode}|{result.PlayerName}|{address.Data!.Ip}:{address.Data!.Port}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "进服失败");
            return $"{result.InviteCode}|{result.PlayerName}|进服失败:{ex.Message}";
        }
    }
    private async Task<string> HandleReloginCommandAsync(CommandResult result)
    {
        var accountInfo = _accountManager.GetAccount(result.InviteCode);
        if (accountInfo == null)
        {
            return $"{result.InviteCode}|{result.PlayerName}|未找到登录信息";
        }
        try
        {
            if (!string.IsNullOrEmpty(accountInfo.Cookie))
            {
                var (authOtp, _) = _wpfLauncher.LoginWithCookie(accountInfo.Cookie);
                accountInfo.EntityId = authOtp.EntityId;
                accountInfo.Token = authOtp.Token;
                accountInfo.Time = DateTime.Now;
                _accountManager.SaveAccount(accountInfo);
                Log.Information("Cookie重新登录成功: {Id}", authOtp.EntityId);
                return $"{result.InviteCode}|{result.PlayerName}|登录成功:{authOtp.EntityId}";
            }
            else
            {
                var mPayUser = await _wpfLauncher.LoginWithEmailAsync(accountInfo.Email, accountInfo.Password);
                var device = _wpfLauncher.MPay.GetDevice();
                var cookieRequest = WPFLauncher.GenerateCookie(mPayUser, device);
                var (authOtp, _) = _wpfLauncher.LoginWithCookie(cookieRequest);
                accountInfo.EntityId = authOtp.EntityId;
                accountInfo.Token = authOtp.Token;
                accountInfo.Time = DateTime.Now;
                _accountManager.SaveAccount(accountInfo);
                Log.Information("账号密码重新登录成功: {Id}", authOtp.EntityId);
                return $"{result.InviteCode}|{result.PlayerName}|登录成功:{authOtp.EntityId}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "重新登录失败");
            return $"{result.InviteCode}|{result.PlayerName}|登录失败:{ex.Message}";
        }
    }
    private async Task<string> HandleTokenLoginCommandAsync(CommandResult result)
    {
        if (result.Parameters.Length < 1)
        {
            return $"{result.InviteCode}|{result.PlayerName}|参数格式错误，请提供加密的token值";
        }
        string token, entityId;
        try
        {
            if (string.IsNullOrEmpty(result.InviteCode))
            {
                return $"{result.InviteCode}|{result.PlayerName}|邀请码不能为空";
            }
            var encryptedParam = result.Parameters[0];
            (token, entityId) = EncryptionUtil.Decrypt(encryptedParam);
            var allAccounts = _accountManager.GetAllAccounts();
            var existingAccount = allAccounts.FirstOrDefault(a => a.Token == token);
            if (existingAccount != null)
            {
                existingAccount.InviteCode = result.InviteCode;
                existingAccount.PlayerName = result.PlayerName;
                existingAccount.Time = DateTime.Now;
                _accountManager.SaveAccount(existingAccount);
                Log.Information("使用token登录成功（现有账号）: {Id}", existingAccount.EntityId);
                return $"{result.InviteCode}|{result.PlayerName}|登录成功:{existingAccount.EntityId}";
            }
            else if (!string.IsNullOrEmpty(entityId))
            {
                var newAccount = new CAccountInfo
                {
                    Id = entityId,
                    InviteCode = result.InviteCode,
                    PlayerName = result.PlayerName,
                    Email = string.Empty,
                    Password = string.Empty,
                    EntityId = entityId,
                    Token = token,
                    Time = DateTime.Now
                };
                _accountManager.SaveAccount(newAccount);
                Log.Information("使用token登录成功（新账号）: {Id}", entityId);
                return $"{result.InviteCode}|{result.PlayerName}|登录成功:{entityId}";
            }
            else
            {
                return $"{result.InviteCode}|{result.PlayerName}|登录失败:请提供完整的token和用户ID信息";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "使用token登录失败");
            return $"{result.InviteCode}|{result.PlayerName}|登录失败:{ex.Message}";
        }
    }
    private async Task<string> GetCrcSaltAsync()
    {
        await InitializeAsync();
        Log.Information("使用存储的 CRC Salt: {Salt}", _crcSalt);
        return _crcSalt;
    }

    private async Task<string> SendTcpCommandAsync(string command)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync("127.0.0.1", 3001);
            using var stream = client.GetStream();
            var data = System.Text.Encoding.UTF8.GetBytes(command + "\n");
            await stream.WriteAsync(data);
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer);
            var response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            Log.Information("向127.0.0.1:3001发送命令成功，响应: {Response}", response);
            return response;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "向127.0.0.1:3001发送命令失败");
            throw;
        }
    }

    private async Task<string> HandleLoginCommandAsync(CommandResult result)
    {
        try
        {
            if (string.IsNullOrEmpty(result.InviteCode))
            {
                return $"{result.InviteCode}|{result.PlayerName}|登录失败:邀请码不能为空";
            }
            var remainingPoints = await CheckInviteCodePointsAsync(result.InviteCode);
            if (remainingPoints <= 0)
            {
                return $"{result.InviteCode}|{result.PlayerName}|邀请码点数不足无法Login";
            }
            if (result.Parameters.Length < 2)
            {
                return $"{result.InviteCode}|{result.PlayerName}|登录失败:参数格式错误，缺少用户名或密码";
            }
            var username = result.Parameters[0];
            var password = result.Parameters[1];
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return $"{result.InviteCode}|{result.PlayerName}|登录失败:用户名或密码不能为空";
            }
            var tcpCommand = $"login|{username}|{password}|{result.InviteCode}|{result.PlayerName}";
            var response = await SendTcpCommandAsync(tcpCommand);
            Log.Information("转发login命令到Generate处理成功，响应: {Response}", response);
            return response;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "转发login命令到Generate处理失败");
            return $"{result.InviteCode}|{result.PlayerName}|登录失败:{ex.Message}";
        }
    }
    #region 邀请码点数管理
    private async Task<int> CheckInviteCodePointsAsync(string inviteCode)
    {
        try
        {
            return -1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "检查邀请码点数失败: {InviteCode}", inviteCode);
            return -1;
        }
    }
    private async Task<bool> DeductInviteCodePointsAsync(string inviteCode, int points)
    {
        try
        {
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "扣除邀请码点数失败: {InviteCode}, 点数: {Points}", inviteCode, points);
            return false;
        }
    }
    #endregion
    #region API 响应模型
    private class NewApiResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("data")]
        public object Data { get; set; }
    }
    private class NewApiResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }
    private class NewPointsData
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("points")]
        public int Points { get; set; }
    }
    private class DeductPointsData
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("old_points")]
        public int OldPoints { get; set; }
        [JsonPropertyName("deduct_points")]
        public int DeductPoints { get; set; }
        [JsonPropertyName("new_points")]
        public int NewPoints { get; set; }
    }
    #endregion
}