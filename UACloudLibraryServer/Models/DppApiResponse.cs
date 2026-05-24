using System;
using System.Collections.Generic;

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

    public sealed record ApiResponse<T>(
        string statusCode,
        T payload = default,
        ApiResult result = null
    );
}
