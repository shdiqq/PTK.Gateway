using System.ComponentModel.DataAnnotations;

namespace PTK.Gateway.Domain.Options;

public sealed class JwtOptions
{
  [Required, MinLength(32)]
  public string Secret { get; set; } = "";

  [Required]
  public string Issuer { get; set; } = "";

  [Required]
  public string Audience { get; set; } = "";
}
