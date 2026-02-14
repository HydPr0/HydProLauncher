using System.Text;
namespace HydPro.Launcher;
public class CommandParser
{
    public static CommandResult Parse(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return new CommandResult
            {
                Success = false,
                Error = "命令不能为空"
            };
        }
        var parts = command.Split('|');
        if (parts.Length < 3)
        {
            return new CommandResult
            {
                Success = false,
                Error = "命令格式错误"
            };
        }
        var commandName = parts[0];
        var inviteCode = parts[^2];
        var playerName = parts[^1];
        var parameters = parts.Skip(1).Take(parts.Length - 3).ToArray();
        return new CommandResult
        {
            Success = true,
            Command = commandName,
            Parameters = parameters,
            InviteCode = inviteCode,
            PlayerName = playerName
        };
    }
}
public class CommandResult
{
    public bool Success { get; set; }
    public string Command { get; set; } = string.Empty;
    public string[] Parameters { get; set; } = Array.Empty<string>();
    public string InviteCode { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}