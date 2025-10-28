namespace PTK.Gateway.Domain.Options;

public sealed class ApiKeysOptions
{
  public Dictionary<string, string> Values { get; set; } =
    new(StringComparer.OrdinalIgnoreCase);
}
