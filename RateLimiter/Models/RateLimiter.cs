namespace RateLimiter.Models
{
    public class RateLimiter
    {
        /// <summary>
        /// Omogućava ili onemogućava rate limiting
        /// </summary>
        public bool RequestLimiterEnabled { get; set; } = true;

        /// <summary>
        /// Vremenski prozor u sekundama
        /// </summary>
        public int DefaultRequestLimitMs { get; set; } = 1000;

        /// <summary>
        /// Maksimalni broj zahteva po vremenskom prozoru
        /// </summary>
        public int DefaultRequestLimitCount { get; set; } = 5;


        /// <summary>
        /// Lista endpointa sa svojim konfiguracijama
        /// </summary>
        public List<EndpointLimits> EndpointLimits { get; set; } = new();
    }
}
