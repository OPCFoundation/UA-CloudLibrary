using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Opc.Ua.Cloud.Library.Models
{
    public static class DppApiStatusCodes
    {
        public const string Success = "Success";
        public const string SuccessCreated = "SuccessCreated";
        public const string SuccessAccepted = "SuccessAccepted";
        public const string SuccessNoContent = "SuccessNoContent";

        public const string ClientErrorBadRequest = "ClientErrorBadRequest";
        public const string ClientNotAuthorized = "ClientNotAuthorized";
        public const string ClientForbidden = "ClientForbidden";
        public const string ClientMethodNotAllowed = "ClientMethodNotAllowed";
        public const string ClientErrorResourceNotFound = "ClientErrorResourceNotFound";
        public const string ClientResourceConflict = "ClientResourceConflict";

        public const string ServerInternalError = "ServerInternalError";
        public const string ServerNotImplemented = "ServerNotImplemented";
        public const string ServerErrorBadGateway = "ServerErrorBadGateway";
    }

    public sealed record ApiMessage(
        string messageType,   // Info | Warning | Error | Exception (draft enumerates these types)
        string text,
        string code = null,
        string correlationId = null,
        DateTimeOffset? timestamp = null
    );

    public sealed record ApiResult(List<ApiMessage> message = null);

    /// <summary>
    /// The EN 18222 response envelope.
    /// </summary>
    /// <remarks>
    /// <paramref name="esdc"/> is a sibling of <paramref name="payload"/> rather than part of it: the
    /// ESDC signs the payload, so nesting it inside what it signs would be self-referential. It is
    /// carried in the body rather than a response header because the signed artifact embeds the whole
    /// DPP, which readily exceeds common server and proxy header size limits; a compact
    /// <c>X-DPP-ESDC-KeyId</c> header is still returned so a verifier can select its trust anchor
    /// without parsing the body. Omitted entirely when null, so non-DPP responses are unchanged.
    /// </remarks>
    public sealed record ApiResponse<T>(
        string statusCode,
        T payload = default,
        ApiResult result = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        Pagination pagination = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        ElectronicSignedDataConstruct esdc = null
    );
}
