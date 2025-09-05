namespace DataPlane.Sdk.Core.Domain.Model;

public class ProvisionResource
{
    public required string Flow { get; init; }
    public required string Type { get; init; }
    public required DataAddress DataAddress { get; init; }
    public IDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}
