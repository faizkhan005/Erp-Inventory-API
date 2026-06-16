namespace ERPInventoryApi.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // Headers containing sensitive data — never log these
    private static readonly HashSet<string> SensitiveHeaders =
        new(StringComparer.OrdinalIgnoreCase) { "Authorization", "Cookie", "X-Api-Key" };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log request
        var safeHeaders = context.Request.Headers
            .Where(h => !SensitiveHeaders.Contains(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        context.Request.EnableBuffering(); // allow body to be read multiple times
        var requestBody = await ReadBodyAsync(context.Request.Body);
        context.Request.Body.Position = 0;  // rewind for the actual handler

        _logger.LogInformation(
            "HTTP {Method} {Path} | Headers: {Headers} | Body: {Body}",
            context.Request.Method,
            context.Request.Path,
            safeHeaders,
            requestBody);

        // Capture response body
        var originalBody = context.Response.Body;
        await using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context);

        memoryStream.Position = 0;
        var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        _logger.LogInformation(
            "HTTP {StatusCode} | Response Body: {Body}",
            context.Response.StatusCode,
            responseBody.Length > 500 ? responseBody[..500] + "…" : responseBody);
    }

    private static async Task<string> ReadBodyAsync(Stream body)
    {
        if (!body.CanRead || !body.CanSeek) return "[non-readable stream]";
        using var reader = new StreamReader(body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
