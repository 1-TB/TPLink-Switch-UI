namespace TPLinkWebUI.Configuration
{
    /// <summary>
    /// Configuration settings for switch communication
    /// </summary>
    public class SwitchConfiguration
    {
        /// <summary>
        /// Default connection timeout in seconds
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Login operation timeout in seconds
        /// </summary>
        public int LoginTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Test connection timeout in seconds
        /// </summary>
        public int TestConnectionTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Session cookie expiration time in hours
        /// </summary>
        public int SessionCookieExpirationHours { get; set; } = 1;

        /// <summary>
        /// Maximum retry attempts for failed operations
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in seconds
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 1;

        /// <summary>
        /// Enable rate limiting for API requests
        /// </summary>
        public bool EnableRateLimiting { get; set; } = true;

        /// <summary>
        /// Maximum requests allowed per minute
        /// </summary>
        public int MaxRequestsPerMinute { get; set; } = 30;
    }

    /// <summary>
    /// Security-related configuration settings
    /// </summary>
    public class SecurityConfiguration
    {
        /// <summary>
        /// Whether to log sensitive data (passwords, tokens, etc.)
        /// </summary>
        public bool LogSensitiveData { get; set; } = false;

        /// <summary>
        /// Require HTTPS for all connections
        /// </summary>
        public bool RequireHttps { get; set; } = false;

        /// <summary>
        /// Maximum login attempts before lockout
        /// </summary>
        public int MaxLoginAttempts { get; set; } = 5;

        /// <summary>
        /// Lockout duration in minutes after max attempts reached
        /// </summary>
        public int LockoutDurationMinutes { get; set; } = 15;
    }

    /// <summary>
    /// CORS configuration settings
    /// </summary>
    public class CorsConfiguration
    {
        /// <summary>
        /// List of allowed origins for CORS
        /// </summary>
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    }
}