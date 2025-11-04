namespace RateLimiter.Models
{
    public record EndpointLimits
    {

        /// <summary>
        /// Endpoint
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;


        /// <summary>
        /// Endpoint request limit count 
        /// </summary>
        public int RequestLimitCount { get; set; }


        /// <summary>
        /// Endpoint request limit 
        /// </summary>
        public int RequestLimitMs { get; set; }
    }
}
