using Microsoft.AspNetCore.HttpOverrides;

using Ocelot.DependencyInjection;
using Ocelot.Provider.Polly;

using PTK.Gateway.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

/* ---------- load ocelot & bind options ---------- */
builder.Configuration.AddJsonFile("config/ocelot.json", optional: false, reloadOnChange: true);

JwtOptions jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new();
CorsOptions corsOpt = builder.Configuration.GetSection("Cors").Get<CorsOptions>() ?? new();
LokiOptions lokiOpt = builder.Configuration.GetSection("Loki").Get<LokiOptions>() ?? new();
SecurityOptions security = builder.Configuration.GetSection("Security").Get<SecurityOptions>() ?? new();

// (opsional) juga expose via DI
builder.Services
  .AddOptions<JwtOptions>()
  .Bind(builder.Configuration.GetSection("Jwt"))
  .ValidateDataAnnotations()
  .Validate(o => o.Schemes is { Count: > 0 }, "Jwt:Schemes tidak boleh kosong")
  .Validate(o => o.Schemes!.All(s =>
      (!string.IsNullOrWhiteSpace(s.Secret) && s.Secret.Length >= 32)
   || !string.IsNullOrWhiteSpace(s.Authority)),
   "Jwt: tiap scheme harus punya Secret(>=32) atau Authority")
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

builder.Services.AddOptions<ApiKeysOptions>()
  .Configure<IConfiguration>((opt, cfg) =>
  {
    opt.Values = builder.Configuration
      .GetSection("ApiKeys")
      .Get<Dictionary<string, string>>()
      ?? new(StringComparer.OrdinalIgnoreCase);
  })
  .Validate(o => builder.Environment.IsDevelopment() || (o.Values?.Count ?? 0) > 0,
            "ApiKeys: minimal satu key pada non-Development")
  .ValidateOnStart();

/* ---------- Tambah service ke DI container ---------- */
// logging dengan Serilog + Loki
builder.Host.UseGatewaySerilog(lokiOpt, builder.Environment);
// auth dengan JWT bearer
builder.Services.AddGatewayJwtAuth(jwtOpt);
// cors
builder.Services.AddGatewayCors(corsOpt);
// ocelot + polly
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
WebApplication app = builder.Build();

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
  _ = app.MapGatewayDebugEndpoints(jwtOpt);
}

/* ---------- cabang /api (hardening + allow-list + Ocelot) ---------- */
app.UseGatewayApiBranch(security);

app.Run();
