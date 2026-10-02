namespace HireHub.Middleware
{
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionLoggingMiddleware> _logger;

        public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception on {Method} {Path} — User: {User}",
                    context.Request.Method,
                    context.Request.Path,
                    context.User?.Identity?.Name ?? "Anonymous");

                throw;   // rethrow so the exception handler still shows the 500 page
            }
        }
    }
}