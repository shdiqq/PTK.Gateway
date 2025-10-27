using System.Text;
using System.Text.Json;

namespace PTK.Gateway.Api.Security;

internal static class JwtForwardingHelper
{
  public static string? TryReadIssuerFromAuthHeader(HttpContext ctx)
  {
    var auth = ctx.Request.Headers["Authorization"].ToString();
    if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
      return null;

    var token = auth.Substring("Bearer ".Length).Trim();
    var parts = token.Split('.');
    if (parts.Length < 2) return null;

    try
    {
      var payload = Base64UrlDecode(parts[1]);
      using var doc = JsonDocument.Parse(payload);
      return doc.RootElement.TryGetProperty("iss", out var iss) ? iss.GetString() : null;
    }
    catch { return null; }
  }

  private static string Base64UrlDecode(string s)
  {
    s = s.Replace('-', '+').Replace('_', '/');
    switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
    return Encoding.UTF8.GetString(Convert.FromBase64String(s));
  }
}
