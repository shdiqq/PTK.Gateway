using System.ComponentModel.DataAnnotations;

namespace PTK.Gateway.Domain.Options;

public sealed class LokiOptions
{
  // Boleh null/empty (artinya: non-Prod atau tidak kirim ke Loki).
  // Jika diisi, harus URL valid (http/https).
  [Url]
  public string? Url { get; set; }
}
