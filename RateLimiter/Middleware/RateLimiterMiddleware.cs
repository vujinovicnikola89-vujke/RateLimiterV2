using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateLimiterConfig = RateLimiter.Models.RateLimiter;

namespace RateLimiter.Middleware
{
    public class RateLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimiterMiddleware> _logger;
        private readonly RateLimiterConfig _config;
        private static readonly Dictionary<string, (int Count, DateTime ResetTime)> _requests = new();

        public RateLimiterMiddleware(
            RequestDelegate next,
            ILogger<RateLimiterMiddleware> logger,
            IOptions<RateLimiterConfig> options)
        {
            _next = next;
            _logger = logger;
            _config = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_config.RequestLimiterEnabled)
            {
                await _next(context);
                return;
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path.ToString();

            var endpointConfig = _config.EndpointLimits.FirstOrDefault(e => path.StartsWith(e.Endpoint, StringComparison.OrdinalIgnoreCase));

            var limit = endpointConfig?.RequestLimitCount ?? _config.DefaultRequestLimitCount;
            var window = TimeSpan.FromMilliseconds(endpointConfig?.RequestLimitMs ?? _config.DefaultRequestLimitMs);

            var key = $"{ip}:{path}";

            lock (_requests)
            {
                if (_requests.TryGetValue(key, out var entry))
                {
                    if (entry.ResetTime > DateTime.UtcNow)
                    {
                        if (entry.Count >= limit)
                        {
                            context.Response.StatusCode = 429;
                            //context.Response.Headers["Retry-After"] = (entry.ResetTime - DateTime.UtcNow).TotalSeconds.ToString("F0");
                            _logger.LogWarning("Rate limit exceeded for {Key}", key);
                            return;
                        }

                        _requests[key] = (entry.Count + 1, entry.ResetTime);
                    }
                    else
                    {
                        _requests[key] = (1, DateTime.UtcNow.Add(window));
                    }
                }
                else
                {
                    _requests[key] = (1, DateTime.UtcNow.Add(window));
                }
            }

            await _next(context);
        }
    }
}
