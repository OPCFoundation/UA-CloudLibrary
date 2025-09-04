using System.Text.Json.Serialization;

namespace DataPlane.Sdk.Core.Infrastructure;

public class Lease
{
    [JsonPropertyName("leasedBy")]
    public required string LeasedBy { get; init; }

    [JsonPropertyName("leasedAt")]
    public long LeasedAt { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [JsonPropertyName("leaseDuration")]
    public required long LeaseDurationMillis { get; init; }

    [JsonIgnore]
    public required string EntityId { get; init; }

    public bool IsExpired(long? now = null)
    {
        now ??= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return LeasedAt + LeaseDurationMillis < now;
    }
}
