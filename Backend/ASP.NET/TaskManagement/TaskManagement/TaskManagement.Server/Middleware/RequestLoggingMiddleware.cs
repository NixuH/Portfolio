using System.Security.Claims;

namespace TaskManagement.Server.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var start = DateTime.UtcNow;

            context.Response.Headers["Request-ID"] = requestId;

            var userId =
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "anonymous";

            _logger.LogInformation(
                "REQUEST #{RequestId}; user={UserId}; in; {Method} {Path}",
                requestId,
                userId,
                context.Request.Method,
                context.Request.Path
            );

            try
            {
                await _next(context);
            }
            finally
            {
                var duration =
                    (DateTime.UtcNow - start).TotalMilliseconds;

                _logger.LogInformation(
                    "REQUEST #{RequestId}; user={UserId}; out; {StatusCode} ({Duration}ms)",
                    requestId,
                    userId,
                    context.Response.StatusCode,
                    duration
                );
            }
        }
    }
}