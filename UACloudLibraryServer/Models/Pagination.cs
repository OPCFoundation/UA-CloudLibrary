using System.Text.Json.Serialization;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// Pagination metadata attached to an <see cref="ApiResponse{T}"/> when the spec method
    /// supports paging (EN 18222 — <c>ReadDPPIdsByProductIds</c>).
    /// The pagination data lives alongside, but separately from, the response payload.
    /// </summary>
    public sealed record Pagination(
        [property: JsonPropertyName("nextCursor")] string NextCursor,
        [property: JsonPropertyName("hasMore")] bool HasMore,
        [property: JsonPropertyName("limit")] int? Limit = null
    );
}
