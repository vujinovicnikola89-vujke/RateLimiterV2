using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using RateLimiterConfig = RateLimiter.Models.RateLimiter;

namespace RateLimiter.Middleware
{
    public class RateLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimiterMiddleware> _logger;
        private readonly RateLimiterConfig _config;
        private readonly IMemoryCache _cache;

        public RateLimiterMiddleware(
            RequestDelegate next,
            ILogger<RateLimiterMiddleware> logger,
            IOptions<RateLimiterConfig> options,
            IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _config = options.Value;
            _cache = cache;
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
            var windowMs = endpointConfig?.RequestLimitMs ?? _config.DefaultRequestLimitMs;
            var refillRate = (double)limit / windowMs * 1000; // tokens per second

            var key = $"{ip}:{path}";
            var now = DateTime.UtcNow;
            bool isLimited;

            var state = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(windowMs * 2);
                return new TokenBucket
                {
                    Tokens = limit,
                    LastRefill = now
                };
            });

            lock (state)
            {
                
                var elapsed = (now - state.LastRefill).TotalMilliseconds;
                var refillAmount = elapsed * refillRate / 1000;
                if (refillAmount > 0)
                {
                    state.Tokens = Math.Min(limit, state.Tokens + refillAmount);
                    state.LastRefill = now;
                }

                if (state.Tokens >= 1)
                {
                    state.Tokens -= 1;
                    isLimited = false;
                }
                else
                {
                    isLimited = true;
                }
            }

            if (isLimited)
            {
                _logger.LogWarning("Rate limit exceeded for {Key}", key);
                await Return429Async(context, limit);
                return;
            }

            await _next(context);
        }

        private static async Task Return429Async(HttpContext context, int limit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";


            var response = new
            {
                error = "Too many requests",
                limit,
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private class TokenBucket
        {
            public double Tokens { get; set; }
            public DateTime LastRefill { get; set; }
        }
    }
}
