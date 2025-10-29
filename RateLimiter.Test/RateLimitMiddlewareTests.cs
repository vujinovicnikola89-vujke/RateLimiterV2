using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using RateLimiter.Middleware;
using System.Net;
using RateLimiterConfig = RateLimiter.Models.RateLimiter;
using Microsoft.Extensions.Logging;

namespace RateLimiter.Tests
{
    [TestClass]
    public class RateLimitMiddlewareTests
    {
        private readonly Mock<ILogger<RateLimiterMiddleware>> _loggerMock =  new Mock<ILogger<RateLimiterMiddleware>>();
        private IOptions<RateLimiterConfig> _conf;

        [TestInitialize]
        public void Setup()
        {
            _conf = Options.Create(new RateLimiterConfig
            {
                RequestLimiterEnabled = true,
                DefaultRequestLimitCount = 5,
                DefaultRequestLimitMs = 1000
            });
        }

        [TestMethod]
        public async Task Should_Allow_Request_When_Under_Limit()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new RateLimiterMiddleware(_ => Task.CompletedTask, _loggerMock.Object, _conf);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.AreEqual(200, context.Response.StatusCode == 0 ? 200 : context.Response.StatusCode);
        }


        [TestMethod]
        public async Task Should_Block_Request_When_Over_Limit()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new RateLimiterMiddleware(_ => Task.CompletedTask, _loggerMock.Object, _conf);

            // Act
            await middleware.InvokeAsync(context); // prvi put - dozvoljen
            await middleware.InvokeAsync(context); // drugi put - dozvoljen
            await middleware.InvokeAsync(context); // treci put - dozvoljen
            await middleware.InvokeAsync(context); // cetvrti put - dozvoljen
            await middleware.InvokeAsync(context); // peti put - blokiran

            // Assert
            Assert.AreEqual(429, context.Response.StatusCode);
            //Assert.IsTrue(context.Response.Headers.ContainsKey("Retry-After"));
        }

        [TestMethod]
        public async Task Should_Reset_Counter_After_Window_Expires()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new RateLimiterMiddleware(_ => Task.CompletedTask, _loggerMock.Object, _conf);

            // Act
            await middleware.InvokeAsync(context); // prvi put
            await Task.Delay(5000); // čekamo da istekne window
            await middleware.InvokeAsync(context); // ponovo posle windowa

            // Assert
            Assert.AreNotEqual(429, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task Should_Skip_When_Disabled()
        {
            // Arrange
            var context = CreateHttpContext();
            var disabledOptions = Options.Create(new RateLimiterConfig { RequestLimiterEnabled = false });
            var middleware = new RateLimiterMiddleware(_ => Task.CompletedTask, _loggerMock.Object, disabledOptions);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.AreNotEqual(429, context.Response.StatusCode);
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            return context;
        }
    }
}
