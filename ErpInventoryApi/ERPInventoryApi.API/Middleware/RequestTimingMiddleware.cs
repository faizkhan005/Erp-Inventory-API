using System.Diagnostics;

namespace ERPInventoryApi.API.Middleware;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;

            // Add timing header so clients can see it too
            context.Response.Headers["X-Response-Time-Ms"] = elapsed.ToString();

            var level = elapsed > 1000 ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(level,
                "Request {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                elapsed,
                context.Response.StatusCode);
        }
    }
}
