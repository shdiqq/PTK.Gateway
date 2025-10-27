using System.ComponentModel.DataAnnotations;

namespace PTK.Gateway.Domain.Options;

public sealed class JwtSchemeOptions
{
  [Required] public string Name { get; set; } = "Bearer";
  public string? Issuer { get; set; }
  public string? Audience { get; set; }

  // Pilih salah satu:
  public string? Secret { get; set; }        // HS256/HS512
  public string? Authority { get; set; }     // OIDC (RS256/JWKS)
  public bool RequireHttpsMetadata { get; set; } = true;

  [Range(0, 600)]
  public int ClockSkewSeconds { get; set; } = 120;
}
