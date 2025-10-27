using System.ComponentModel.DataAnnotations;

namespace PTK.Gateway.Domain.Options;

public sealed class JwtOptions
{
  [Required]
  public List<JwtSchemeOptions> Schemes { get; set; } = new();
}
