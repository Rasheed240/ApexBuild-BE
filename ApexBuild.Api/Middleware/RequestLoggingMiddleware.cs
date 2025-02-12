using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ApexBuild.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request);

        _logger.LogInformation(
            "HTTP {Method} {Path} started. QueryString: {QueryString}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        if (!string.IsNullOrEmpty(requestBody) && requestBody.Length < 1000)
        {
            _logger.LogDebug("Request Body: {RequestBody}", requestBody);
        }

        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var responseBodyContent = await ReadResponseBodyAsync(context.Response);
            await responseBody.CopyToAsync(originalBodyStream);

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            if (!string.IsNullOrEmpty(responseBodyContent) && responseBodyContent.Length < 1000)
            {
                _logger.LogDebug("Response Body: {ResponseBody}", responseBodyContent);
            }
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        return text;
    }
}

