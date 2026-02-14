using System.Text.Json.Serialization;
namespace HydPro.Launcher.Entities;
public class CrcSalt
{
    [JsonPropertyName("crcSalt")] public required string Salt { get; set; }
    [JsonPropertyName("gameVersion")] public required string Version { get; set; }
}