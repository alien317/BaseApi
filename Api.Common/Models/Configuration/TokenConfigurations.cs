namespace Api.Common.Models.Configuration
{
    public class JwtConfiguration
    {
        public string? Key { get; set; } = "";
        public string? Issuer { get; set; } = "";
        public RefreshTokenConfiguration? RefreshToken { get; set; } = new();
    }

    public class RefreshTokenConfiguration
    {
        public string? Secret { get; set; } = "";
        public int? RefreshTokenTTL { get; set; } = 0;
    }
}
