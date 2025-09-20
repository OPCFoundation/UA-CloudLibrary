using System.Text.Json.Serialization;

namespace DataPlane.Sdk.Core.Domain.Model;

/// <summary>
///     Represents an abstract result that encapsulates either a successful content value or a failure value.
/// </summary>
/// <typeparam name="TContent">The type of the content returned on success.</typeparam>
/// <typeparam name="TFailure">The type of the failure returned on error.</typeparam>
/// <param name="content">The content value if the operation succeeded; otherwise, <c>null</c>.</param>
/// <param name="failure">The failure value if the operation failed; otherwise, <c>null</c>.</param>
public abstract class AbstractResult<TContent, TFailure>(TContent? content, TFailure? failure)
{
    public TContent? Content { get; set; } = content;
    public TFailure? Failure { get; set; } = failure;

    [JsonIgnore]
    public bool IsSucceeded => Failure == null;

    [JsonIgnore]
    public bool IsFailed => !IsSucceeded;
}
