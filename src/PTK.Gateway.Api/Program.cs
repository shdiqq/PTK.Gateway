using System.Text;
using System.Security.Claims;

using Microsoft.AspNetCore.HttpOverrides;

using Ocelot.DependencyInjection;
using Ocelot.Provider.Polly;

using PTK.Gateway.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

/* ---------- load ocelot & bind options ---------- */
builder.Configuration.AddJsonFile("config/ocelot.json", optional: false, reloadOnChange: true);

var jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new();
var corsOpt = builder.Configuration.GetSection("Cors").Get<CorsOptions>() ?? new();
var lokiOpt = builder.Configuration.GetSection("Loki").Get<LokiOptions>() ?? new();
var security = builder.Configuration.GetSection("Security").Get<SecurityOptions>() ?? new();
var funnelOpt = builder.Configuration.GetSection("Funnel").Get<FunnelOptions>() ?? new();

// (opsional) juga expose via DI
builder.Services
  .AddOptions<JwtOptions>()
  .Bind(builder.Configuration.GetSection("Jwt"))
  .ValidateDataAnnotations()
  .Validate(o => builder.Environment.IsDevelopment() || (o.Secret?.Length ?? 0) >= 32,
            "Jwt:Secret minimal 32 karakter pada non-Development")
  .ValidateOnStart();
builder.Services
  .AddOptions<CorsOptions>()
  .Bind(builder.Configuration.GetSection("Cors"))
  .Validate(o => o.AllowedOrigins is not null, "Cors:AllowedOrigins null")
  .ValidateOnStart();
builder.Services
  .AddOptions<LokiOptions>()
  .Bind(builder.Configuration.GetSection("Loki"))
  // Url boleh null/empty; kalau diisi harus http/https valid
  .Validate(o => string.IsNullOrWhiteSpace(o.Url) ||
                 Uri.TryCreate(o.Url, UriKind.Absolute, out var u) &&
                 (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps),
            "Loki:Url harus http/https yang valid")
  .ValidateOnStart();
builder.Services
  .AddOptions<SecurityOptions>()
  .Bind(builder.Configuration.GetSection("Security"))
  .ValidateOnStart();

builder.Services
  .AddOptions<FunnelOptions>()
  .Bind(builder.Configuration.GetSection("Funnel"))
  // Di Production, dorong agar tidak kosong (opsional—sesuaikan kebijakanmu)
  .Validate(o => builder.Environment.IsDevelopment() || !string.IsNullOrWhiteSpace(o.ApiKey),
            "Funnel:ApiKey wajib diisi pada non-Development")
  .ValidateOnStart();

/* ---------- logging / auth / cors / ocelot ---------- */
builder.Host.UseGatewaySerilog(lokiOpt, builder.Environment);
builder.Services.AddGatewayJwtAuth(jwtOpt);
builder.Services.AddGatewayCors(corsOpt);
builder.Services.AddOcelot(builder.Configuration).AddPolly();

// ---------- Forwarded Headers (di belakang reverse proxy) ----------
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
  opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  // biar terima header dari proxy mana pun (opsi praktis jika kamu tahu perimeter jaringanmu)
  opts.KnownNetworks.Clear();
  opts.KnownProxies.Clear();
  // opsional: batasi kedalaman chain, kalau di depan cuma 1 proxy
  // opts.ForwardLimit = 1;
});

/* ---------- build ---------- */
var app = builder.Build();

app.UseGlobalExceptionProblem(app.Environment.IsDevelopment());
app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();
app.UseAppInfoHeaders();

// request timing log (JSON compact)
app.UseGatewayRequestTimingLogging();

// health boleh tetap terbuka (read-only)
app.MapGatewayHealthEndpoints();

// Endpoint debug hanya saat Development
if (app.Environment.IsDevelopment())
{
  app.MapGet("/_token", (string? sub, string? role) =>
  {
    sub ??= "user1";
    role ??= "user";
    var claims = new List<Claim> {
      new Claim(AuthClaimNames.Subject, sub),
      new Claim(AuthClaimNames.Role, role)
    };
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.Secret));
    var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(jwtOpt.Issuer, jwtOpt.Audience, claims,
      expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: creds);
    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Json(new { token = jwt });
  });

  app.MapGet("/_backend/echo", (HttpContext ctx) =>
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
  });

  app.MapGet("/_backend/slow", async (HttpContext ctx) =>
  {
    if (!int.TryParse(ctx.Request.Query["ms"], out var ms)) ms = 1000;
    await Task.Delay(ms);
    return Results.Json(new { ok = true, path = ctx.Request.Path.Value, waited_ms = ms });
  });

  app.MapGet("/_backend/secure", (ClaimsPrincipal user) =>
  {
    var sub = user.FindFirstValue(AuthClaimNames.Subject) ?? "(unknown)";
    var role = user.FindFirstValue(AuthClaimNames.Role) ?? "user";
    return Results.Json(new { ok = true, sub, role, at = DateTime.UtcNow });
  }).RequireAuthorization();

  app.MapGet("/_backend/aborted", async (HttpContext ctx) =>
  {
    await Task.Delay(500);
    ctx.Abort();
    return Results.Empty;
  });
}

/* ---------- cabang /api (hardening + allow-list + Ocelot) ---------- */
app.UseGatewayApiBranch(security, funnelOpt);

app.Run();
