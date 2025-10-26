using System.Security.Claims;
using System.Text;

namespace PTK.Gateway.Api.Extensions;

public static class DebugEndpointsExtensions
{
  /// <summary>
  /// Map debug endpoints yang hanya untuk Development.
  /// - GET /_token?sub=...&role=...
  /// - GET /_backend/echo
  /// - GET /_backend/slow?ms=...
  /// - GET /_backend/secure (RequireAuthorization)
  /// - GET /_backend/aborted  (simulasi aborted request)
  /// </summary>
  public static IEndpointRouteBuilder MapGatewayDebugEndpoints(
    this IEndpointRouteBuilder app, JwtOptions jwtOpt)
  {
    // group agar rapi di OpenAPI kalau nanti ditambah
    var root = app.MapGroup("/")
                  .WithTags("debug");

    root.MapGet("/_token", (string? sub, string? role) =>
    {
      sub ??= "user1";
      role ??= "user";

      var claims = new List<Claim>
      {
        new(AuthClaimNames.Subject, sub),
        new(AuthClaimNames.Role, role)
      };

      var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.Secret));
      var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

      var token = new JwtSecurityToken(jwtOpt.Issuer, jwtOpt.Audience, claims,
        expires: DateTime.UtcNow.AddMinutes(10),
        signingCredentials: creds);

      var jwt = new JwtSecurityTokenHandler().WriteToken(token);
      return Results.Json(new { token = jwt });
    })
    .WithName("DebugToken")
    .WithDescription("Dev-only: generate JWT quickly");

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

    root.MapGet("/_backend/slow", async (HttpContext ctx) =>
    {
      if (!int.TryParse(ctx.Request.Query["ms"], out var ms)) ms = 1000;
      await Task.Delay(ms);
      return Results.Json(new { ok = true, path = ctx.Request.Path.Value, waited_ms = ms });
    })
    .WithName("DebugSlow");

    root.MapGet("/_backend/secure", (ClaimsPrincipal user) =>
    {
      var sub = user.FindFirstValue(AuthClaimNames.Subject) ?? "(unknown)";
      var role = user.FindFirstValue(AuthClaimNames.Role) ?? "user";
      return Results.Json(new { ok = true, sub, role, at = DateTime.UtcNow });
    })
    .RequireAuthorization()
    .WithName("DebugSecure");

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
