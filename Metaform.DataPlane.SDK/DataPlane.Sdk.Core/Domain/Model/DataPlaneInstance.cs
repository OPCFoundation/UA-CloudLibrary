namespace DataPlane.Sdk.Core.Domain.Model;

/// <summary>
///     Represents a concrete instance of a data plane with its self-description like state, allowed types, properties, and
///     a URL.
/// </summary>
/// <param name="id">The unique identifier for the data plane instance.</param>
public class DataPlaneInstance(string id) : StatefulEntity<DataPlaneState>(id)
{
    public required ICollection<string> AllowedSourceTypes { get; set; } = new List<string>();
    public required ICollection<string> AllowedTransferTypes { get; set; } = new List<string>();
    public ICollection<string> DestinationProvisionTypes { get; set; } = new List<string>();
    public long LastActive { get; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public required Uri? Url { get; set; }
}

public enum DataPlaneState
{
    Registered = 100,
    Available = 200,
    Unavailable = 300,
    Unregistered = 400
}
