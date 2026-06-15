#nullable enable
using System.Text.Json.Serialization;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// Pagination metadata attached to an <see cref="ApiResponse{T}"/> when the spec method
    /// supports paging (EN 18222 — <c>ReadDPPIdsByProductIds</c>).
    /// The pagination data lives alongside, but separately from, the response payload.
    /// </summary>
    /// <remarks>
    /// <see cref="NextCursor"/> is nullable: it is only populated when <see cref="HasMore"/> is
    /// <c>true</c>. The <see cref="JsonIgnoreAttribute"/> with
    /// <see cref="JsonIgnoreCondition.WhenWritingNull"/> keeps the serialized envelope from
    /// emitting <c>"nextCursor": null</c> on the final page, which would otherwise be
    /// indistinguishable from a missing cursor on the wire.
    /// </remarks>
    public sealed record Pagination(
        [property: JsonPropertyName("nextCursor")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? NextCursor,
        [property: JsonPropertyName("hasMore")] bool HasMore,
        [property: JsonPropertyName("limit")] int? Limit = null
    );
}
