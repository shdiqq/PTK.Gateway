namespace PTK.Gateway.Domain.Options;

public sealed class CorsOptions
{
  public string[] AllowedOrigins { get; set; } = [];
}
