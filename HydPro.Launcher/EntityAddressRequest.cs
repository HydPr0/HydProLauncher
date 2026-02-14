using System.Text.Json.Serialization;
namespace HydPro.Launcher;
public class EntityAddressRequest
{
    [JsonPropertyName("item_id")] public string ItemId { get; set; } = string.Empty;
}