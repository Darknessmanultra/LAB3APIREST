
using System.Diagnostics;
using Serilog.Context;

namespace Shortly.Application.Helpers;
public sealed class RequestTracingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestTracingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers["X-Correlation-Id"]
                .FirstOrDefault()
            ?? Ulid.NewUlid().ToString();

        context.Response.Headers["X-Correlation-Id"] =
            correlationId;

        using (LogContext.PushProperty(
                   "CorrelationId",
                   correlationId))
        using (LogContext.PushProperty(
                   "TraceId",
                   Activity.Current?.TraceId.ToString()))
        using (LogContext.PushProperty(
                   "SpanId",
                   Activity.Current?.SpanId.ToString()))
        using (LogContext.PushProperty(
                   "RequestPath",
                   context.Request.Path))
        using (LogContext.PushProperty(
                   "RequestMethod",
                   context.Request.Method))
        using (LogContext.PushProperty(
                   "RemoteIp",
                   context.Connection.RemoteIpAddress?.ToString()))
        using (LogContext.PushProperty(
                   "UserAgent",
                   context.Request.Headers.UserAgent.ToString()))
        {
            await _next(context);
        }
    }
}