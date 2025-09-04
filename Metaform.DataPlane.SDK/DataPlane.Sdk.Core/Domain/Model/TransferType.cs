namespace DataPlane.Sdk.Core.Domain.Model;

/// <summary>
///     Represents a transfer type.
/// </summary>
/// <param name="DestinationType">The physical location where data is supposed to go</param>
/// <param name="FlowType">push or pull</param>
/// <param name="ResponseChannel">optional: the type designation for the response channel</param>
public record TransferType(string DestinationType, FlowType FlowType, string? ResponseChannel = null);

public enum FlowType
{
    Push,
    Pull
}
