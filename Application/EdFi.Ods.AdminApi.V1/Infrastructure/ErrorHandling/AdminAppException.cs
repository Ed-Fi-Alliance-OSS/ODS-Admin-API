using System;
using System.Net;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.ErrorHandling;

public interface IAdminApiException
{
    string? Message { get; }
    string? StackTrace { get; }
    HttpStatusCode? StatusCode { get; }
    bool AllowFeedback { get; }
    bool IsStackTraceRelevant { get; }
}

public class AdminApiException : Exception, IAdminApiException
{
    public AdminApiException(string message) : base(message) { }
    public AdminApiException(string message, Exception innerException) : base(message, innerException) { }

    public HttpStatusCode? StatusCode { get; set; }
    public bool AllowFeedback { get; set; } = true;
    public bool IsStackTraceRelevant { get; set; } = false;
}
