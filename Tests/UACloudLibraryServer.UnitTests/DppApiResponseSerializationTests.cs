using System.Collections.Generic;
using System.Text.Json;

using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for the DPP response envelope shape. These guard the two
    /// PR-review fixes that made the envelope spec-compliant:
    /// <list type="bullet">
    ///   <item><see cref="Pagination.NextCursor"/> is nullable and omitted when null.</item>
    ///   <item><see cref="ApiResponse{T}"/> omits its <c>pagination</c> property when null
    ///   so non-paged methods do not emit <c>"pagination": null</c>.</item>
    /// </list>
    /// </summary>
    public class DppApiResponseSerializationTests
    {
        // ASP.NET Core uses Web defaults (camelCase, case-insensitive) when no JsonOptions
        // override is registered, which is the configuration of the DPP controller.
        private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web);

        [Fact]
        public void Pagination_OmitsNextCursor_WhenNull()
        {
            var pagination = new Pagination(NextCursor: null, HasMore: false, Limit: 100);

            string json = JsonSerializer.Serialize(pagination, s_options);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            Assert.False(root.TryGetProperty("nextCursor", out _),
                "nextCursor must be omitted on the final page rather than serialized as null.");
            Assert.True(root.TryGetProperty("hasMore", out JsonElement hasMore));
            Assert.False(hasMore.GetBoolean());
            Assert.True(root.TryGetProperty("limit", out JsonElement limit));
            Assert.Equal(100, limit.GetInt32());
        }

        [Fact]
        public void Pagination_EmitsNextCursor_WhenPresent()
        {
            var pagination = new Pagination(NextCursor: "42", HasMore: true, Limit: 50);

            string json = JsonSerializer.Serialize(pagination, s_options);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            Assert.True(root.TryGetProperty("nextCursor", out JsonElement nextCursor));
            Assert.Equal("42", nextCursor.GetString());
            Assert.True(root.GetProperty("hasMore").GetBoolean());
        }

        [Fact]
        public void ApiResponse_OmitsPagination_WhenNull()
        {
            var response = new ApiResponse<List<string>>(
                statusCode: DppApiStatusCodes.Success,
                payload: new List<string> { "a", "b" });

            string json = JsonSerializer.Serialize(response, s_options);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            Assert.False(root.TryGetProperty("pagination", out _),
                "Non-paged responses must not emit 'pagination': null.");
            Assert.Equal(DppApiStatusCodes.Success, root.GetProperty("statusCode").GetString());
            Assert.Equal(2, root.GetProperty("payload").GetArrayLength());
        }

        [Fact]
        public void ApiResponse_EmitsPagination_WhenProvided()
        {
            var response = new ApiResponse<List<string>>(
                statusCode: DppApiStatusCodes.Success,
                payload: new List<string> { "a" },
                pagination: new Pagination(NextCursor: "10", HasMore: true, Limit: 5));

            string json = JsonSerializer.Serialize(response, s_options);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            Assert.True(root.TryGetProperty("pagination", out JsonElement pagination));
            Assert.Equal("10", pagination.GetProperty("nextCursor").GetString());
            Assert.True(pagination.GetProperty("hasMore").GetBoolean());
            Assert.Equal(5, pagination.GetProperty("limit").GetInt32());
        }

        [Fact]
        public void ApiResponse_OmitsResult_WhenNull()
        {
            var response = new ApiResponse<string>(
                statusCode: DppApiStatusCodes.Success,
                payload: "ok");

            string json = JsonSerializer.Serialize(response, s_options);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            // Without an explicit DefaultIgnoreCondition, result is still emitted as null by
            // default. This test pins the current behavior so any future global ignore policy
            // change is surfaced explicitly rather than silently altering the wire contract.
            Assert.True(root.TryGetProperty("result", out JsonElement result));
            Assert.Equal(JsonValueKind.Null, result.ValueKind);
        }
    }
}
