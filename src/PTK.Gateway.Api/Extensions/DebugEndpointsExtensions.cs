using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace PTK.Gateway.Api.Extensions;

public static class DebugEndpointsExtensions
{
  /// <summary>
  /// Map debug endpoints yang hanya untuk Development.
  /// - GET /_token?sub=...&role=...&scheme=...
  /// - GET /_backend/echo
  /// - GET /_backend/slow?ms=...
  /// - GET /_backend/secure (RequireAuthorization)
  /// - GET /_backend/aborted  (simulasi aborted request)
  /// </summary>
  public static IEndpointRouteBuilder MapGatewayDebugEndpoints(
    this IEndpointRouteBuilder app, JwtOptions jwtOpt)
  {
    var root = app.MapGroup("/")
                  .WithTags("debug");

    // === Dev token generator (HANYA untuk scheme dengan Secret/HMAC) ===
    root.MapGet("/_token", (string? sub, string? role, string? scheme) =>
    {
      sub ??= "user1";
      role ??= "user";

      var sel = string.IsNullOrWhiteSpace(scheme)
        ? jwtOpt.Schemes.FirstOrDefault()
        : jwtOpt.Schemes.FirstOrDefault(s => string.Equals(s.Name, scheme, StringComparison.OrdinalIgnoreCase));

      if (sel is null)
        return Results.Problem(statusCode: 400, title: "Invalid scheme",
          detail: "Jwt.Schemes kosong atau nama scheme tidak ditemukan.");

      if (string.IsNullOrWhiteSpace(sel.Secret))
        return Results.Problem(statusCode: 400, title: "Cannot mint token",
          detail: $"Scheme '{sel.Name}' menggunakan Authority/JWKS (RS256). Gunakan STS asli untuk mendapatkan token.");

      if (string.IsNullOrWhiteSpace(sel.Issuer) || string.IsNullOrWhiteSpace(sel.Audience))
        return Results.Problem(statusCode: 400, title: "Issuer/Audience required",
          detail: $"Scheme '{sel.Name}' belum memiliki Issuer/Audience di konfigurasi.");

      var claims = new List<Claim>
      {
        new(AuthClaimNames.Subject, sub),
        new(AuthClaimNames.Role, role)
      };

      var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sel.Secret));
      var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

      var token = new JwtSecurityToken(
        issuer: sel.Issuer,
        audience: sel.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(10),
        signingCredentials: creds);

      var jwt = new JwtSecurityTokenHandler().WriteToken(token);
      return Results.Json(new
      {
        token = jwt,
        scheme = sel.Name,
        iss = sel.Issuer,
        aud = sel.Audience,
        expires_in_minutes = 10
      });
    })
    .WithName("DebugToken")
    .WithDescription("Dev-only: generate JWT quickly for a specific scheme (only HMAC/Secret).");

    // === Echo downstream headers/query ===
    root.MapGet("/_backend/echo", (HttpContext ctx) =>
    {
      var q = ctx.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
      var apiKey = ctx.Request.Headers[HeaderNames.ApiKey].ToString();
      var clientId = ctx.Request.Headers[HeaderNames.ClientId].ToString();
      var userSub = ctx.Request.Headers[HeaderNames.UserSub].ToString();
      var userRole = ctx.Request.Headers[HeaderNames.UserRole].ToString();

      return Results.Json(new
      {
        ok = true,
        path = ctx.Request.Path.Value,
        query = q,
        downstream_api_key = apiKey,
        downstream_client_id = clientId,
        downstream_user_sub = userSub,
        downstream_user_role = userRole
      });
    })
    .WithName("DebugEcho");

    // === Slow endpoint (simulasi latency) ===
    root.MapGet("/_backend/slow", async (HttpContext ctx) =>
    {
      if (!int.TryParse(ctx.Request.Query["ms"], out var ms)) ms = 1000;
      await Task.Delay(ms);
      return Results.Json(new { ok = true, path = ctx.Request.Path.Value, waited_ms = ms });
    })
    .WithName("DebugSlow");

    // === Secure (butuh Authorization) ===
    root.MapGet("/_backend/secure", (ClaimsPrincipal user) =>
    {
      var sub = user.FindFirstValue(AuthClaimNames.Subject) ?? "(unknown)";
      var role = user.FindFirstValue(AuthClaimNames.Role) ?? "user";
      return Results.Json(new { ok = true, sub, role, at = DateTime.UtcNow });
    })
    .RequireAuthorization()
    .WithName("DebugSecure");

    // === Aborted request simulator ===
    root.MapGet("/_backend/aborted", async (HttpContext ctx) =>
    {
      await Task.Delay(500);
      ctx.Abort();
      return Results.Empty;
    })
    .WithName("DebugAborted");

    return app;
  }
}
