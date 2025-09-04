using static DataPlane.Sdk.Core.Domain.Model.FailureReason;

namespace DataPlane.Sdk.Core.Domain.Model;

public class StatusResult<TContent>(TContent? content, StatusFailure? failure)
    : AbstractResult<TContent, StatusFailure>(content, failure)
{
    public static StatusResult<TContent> Success(TContent? content)
    {
        return new StatusResult<TContent>(content, null);
    }

    public static StatusResult<TContent> Failed(StatusFailure failure)
    {
        return new StatusResult<TContent>(default, failure);
    }

    public static StatusResult<TContent> NotFound()
    {
        return Failed(new StatusFailure {
            Message = "Not Found",
            Reason = FailureReason.NotFound
        });
    }

    public static StatusResult<TContent> Conflict(string message)
    {
        return Failed(new StatusFailure {
            Message = message,
            Reason = FailureReason.Conflict
        });
    }

    public static StatusResult<TContent> FromCode(int resultStatusCode, string? resultReasonPhrase)
    {
        return resultStatusCode switch {
            404 => NotFound(),
            409 => Conflict(resultReasonPhrase ?? "Conflict occurred"),
            500 => Failed(new StatusFailure { Message = resultReasonPhrase ?? "Internal Server Error", Reason = InternalError }),
            503 => Failed(new StatusFailure { Message = resultReasonPhrase ?? "Service Unavailable", Reason = ServiceUnavailable }),
            401 => Failed(new StatusFailure { Message = resultReasonPhrase ?? "Unauthorized", Reason = Unauthorized }),
            403 => Failed(new StatusFailure { Message = resultReasonPhrase ?? "Forbidden", Reason = Forbidden }),
            400 => Failed(new StatusFailure { Message = resultReasonPhrase ?? "Bad Request", Reason = BadRequest }),
            _ => Failed(new StatusFailure { Message = resultReasonPhrase ?? "Unknown Error", Reason = Unrecognized })
        };
    }
}

public class StatusFailure
{
    public required string Message { get; set; }
    public required FailureReason Reason { get; set; }
}

public enum FailureReason
{
    NotFound = 404,
    Conflict = 409,
    InternalError = 500,
    ServiceUnavailable = 503,
    Unauthorized = 401,
    Forbidden = 403,
    BadRequest = 400,
    Unrecognized = 0
}
