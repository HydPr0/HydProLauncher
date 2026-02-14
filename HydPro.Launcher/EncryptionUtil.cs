using System;
using System.Text;
namespace HydPro.Launcher;
public static class EncryptionUtil
{
    private const string Key = "K1yP@ssw0rd";
    public static (string Token, string EntityId) Decrypt(string encryptedString)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedString))
            {
                throw new Exception("none");
            }
            var decryptedString = DecryptString(encryptedString);
            var parts = decryptedString.Split('|');
            if (parts.Length != 2)
            {
                throw new Exception("bad");
            }
            return (parts[0], parts[1]);
        }
        catch (Exception ex)
        {
            throw new Exception($"bad: {ex.Message}");
        }
    }
    private static string DecryptString(string encryptedString)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedString);
        var keyBytes = Encoding.UTF8.GetBytes(Key);
        var decryptedBytes = new byte[encryptedBytes.Length];
        for (int i = 0; i < encryptedBytes.Length; i++)
        {
            decryptedBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    public static string Encrypt(string token, string entityId)
    {
        var combinedString = $"{token}|{entityId}";
        var combinedBytes = Encoding.UTF8.GetBytes(combinedString);
        var keyBytes = Encoding.UTF8.GetBytes(Key);
        var encryptedBytes = new byte[combinedBytes.Length];
        for (int i = 0; i < combinedBytes.Length; i++)
        {
            encryptedBytes[i] = (byte)(combinedBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }
        return Convert.ToBase64String(encryptedBytes);
    }
}