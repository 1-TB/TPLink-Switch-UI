namespace TPLinkWebUI.Models
{
    public class LoginRequest
    {
        public string Host { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? SessionCookie { get; set; }
        public DateTime? CookieExpiration { get; set; }
    }
}