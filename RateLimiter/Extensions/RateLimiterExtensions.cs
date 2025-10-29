using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RateLimiter.Middleware;
using RateLimiterConfig = RateLimiter.Models.RateLimiter;


namespace RateLimiter.Extensions
{
    public static class RateLimiterExtensions
    {
        public static IServiceCollection AddRequestRateLimiting(this IServiceCollection services, Action<RateLimiterConfig>? configure = null)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<RateLimiterConfig>(_ => { });
            }
            return services;
        }

        public static IApplicationBuilder UseRequestRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimiterMiddleware>();
        }
    }
}
