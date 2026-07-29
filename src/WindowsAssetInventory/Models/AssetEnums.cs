using System.Text.Json.Serialization;

namespace WindowsAssetInventory.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetType
{
    Computer,
    Laptop,
    Monitor,
    Peripheral
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetStatus
{
    [JsonStringEnumMemberName("In Stock")]
    InStock,
    Assigned,
    Repair,
    Retired
}
