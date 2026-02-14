using System.Text.Json;
using Serilog;
namespace HydPro.Launcher;
public class AccountManager
{
    private const string FileName = "playercaccount.json";
    private static readonly object _lock = new object();
    private readonly Dictionary<string, CAccountInfo> _accounts;
    public AccountManager()
    {
        _accounts = new Dictionary<string, CAccountInfo>();
        LoadFromFile();
    }
    public CAccountInfo? GetAccount(string inviteCode)
    {
        lock (_lock)
        {
            return _accounts.TryGetValue(inviteCode, out var account) ? account : null;
        }
    }
    public void SaveAccount(CAccountInfo accountInfo)
    {
        lock (_lock)
        {
            _accounts[accountInfo.InviteCode] = accountInfo;
            SaveToFile();
        }
    }
    public bool RemoveAccount(string inviteCode)
    {
        lock (_lock)
        {
            var removed = _accounts.Remove(inviteCode);
            if (removed)
            {
                SaveToFile();
            }
            return removed;
        }
    }
    private void LoadFromFile()
    {
        if (!File.Exists(FileName))
        {
            return;
        }
        try
        {
            var json = File.ReadAllText(FileName);
            var accounts = JsonSerializer.Deserialize<Dictionary<string, CAccountInfo>>(json);
            if (accounts != null)
            {
                foreach (var kvp in accounts)
                {
                    _accounts[kvp.Key] = kvp.Value;
                }
                Log.Information("已从{FileName}加载{Count}个账号信息", FileName, _accounts.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载{FileName}文件失败", FileName);
        }
    }
    private void SaveToFile()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(_accounts, options);
            File.WriteAllText(FileName, json);
            Log.Information("已保存{Count}个账号信息到{FileName}", _accounts.Count, FileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存{FileName}文件失败", FileName);
        }
    }
    public List<CAccountInfo> GetAllAccounts()
    {
        lock (_lock)
        {
            return _accounts.Values.ToList();
        }
    }
}
public class CAccountInfo
{
    public string Id { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Cookie { get; set; } = string.Empty;
    public DateTime Time { get; set; } = DateTime.Now;
}