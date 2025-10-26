using Ocelot.Middleware;

namespace PTK.Gateway.Api.Extensions;

public static class ApiBranchExtensions
{
  public static IApplicationBuilder UseGatewayApiBranch(
    this IApplicationBuilder app,
    SecurityOptions security,
    FunnelOptions funnelOpt)
  {
    app.MapWhen(
      ctx => PathUtils.IsUnderPrefix(ctx.Request.Path, ApiRoutes.Prefix),
      subApp =>
      {
        subApp.Use(async (ctx, next) =>
        {
          // header hygiene
          if (ctx.Request.Headers.ContainsKey(HeaderNames.ApiKey)) ctx.Request.Headers.Remove(HeaderNames.ApiKey);
          if (ctx.Request.Headers.ContainsKey(HeaderNames.ClientId)) ctx.Request.Headers.Remove(HeaderNames.ClientId);
          if (ctx.Request.Headers.ContainsKey(HeaderNames.UserSub)) ctx.Request.Headers.Remove(HeaderNames.UserSub);
          if (ctx.Request.Headers.ContainsKey(HeaderNames.UserRole)) ctx.Request.Headers.Remove(HeaderNames.UserRole);

          // client id
          string? clientId = null;
          if (ctx.User?.Identity?.IsAuthenticated == true)
            clientId = ctx.User.FindFirst(AuthClaimNames.Subject)?.Value
                       ?? ctx.User.FindFirst(AuthClaimNames.ClientId)?.Value;

          if (string.IsNullOrWhiteSpace(clientId))
          {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var ua = ctx.Request.Headers["User-Agent"].FirstOrDefault();
            clientId = ClientIdHasher.FromIpUa(ip, ua);
          }
          ctx.Request.Headers[HeaderNames.ClientId] = clientId!;

          // propagate identity to internal services only
          if (ctx.User?.Identity?.IsAuthenticated == true)
          {
            var sub = ctx.User.FindFirst(AuthClaimNames.Subject)?.Value;
            var role = ctx.User.FindFirst(AuthClaimNames.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(sub)) ctx.Request.Headers[HeaderNames.UserSub] = sub;
            if (!string.IsNullOrWhiteSpace(role)) ctx.Request.Headers[HeaderNames.UserRole] = role;
          }

          // ---- khusus funnel
          var path = ctx.Request.Path.Value ?? "/";
          if (PathUtils.IsUnderPrefix(ctx.Request.Path, ApiRoutes.FunnelPrefix))
          {
            // 1) wajib auth
            if (!(ctx.User?.Identity?.IsAuthenticated ?? false))
            {
              ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
              await ctx.Response.WriteAsJsonAsync(new { ok = false, error = "Unauthorized" });
              return;
            }

            // normalisasi relPath untuk allow-list
            var rel = PathUtils.NormalizeFunnelRelative(ctx.Request.Path);

            // 2) allow-list
            if (!FunnelAllowListPolicy.IsAllowed(ctx.Request.Method, rel))
            {
              ctx.Response.StatusCode = security.HideForbidden ? StatusCodes.Status404NotFound
                                                               : StatusCodes.Status403Forbidden;

              var payload = security.HideForbidden
                ? new { ok = false, error = "Not Found" }
                : new { ok = false, error = "Forbidden: endpoint not allowed via gateway" };

              await ctx.Response.WriteAsJsonAsync(payload);
              return;
            }

            // 3) jangan teruskan identitas ke pihak ketiga
            if (ctx.Request.Headers.ContainsKey(HeaderNames.Authorization)) ctx.Request.Headers.Remove(HeaderNames.Authorization);
            if (ctx.Request.Headers.ContainsKey(HeaderNames.UserSub)) ctx.Request.Headers.Remove(HeaderNames.UserSub);
            if (ctx.Request.Headers.ContainsKey(HeaderNames.UserRole)) ctx.Request.Headers.Remove(HeaderNames.UserRole);

            // 4) paksa client-id anonim
            var ip2 = ctx.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var ua2 = ctx.Request.Headers["User-Agent"].FirstOrDefault();
            var anon = ClientIdHasher.FromIpUa(ip2, ua2);
            ctx.Request.Headers[HeaderNames.ClientId] = anon;

            // 5) injeksi API key
            ctx.Request.Headers[HeaderNames.ApiKey] = funnelOpt.ApiKey;
          }

          // contoh injeksi demo internal
          if (path.StartsWith("/api/echo", StringComparison.OrdinalIgnoreCase))
          {
            ctx.Request.Headers[HeaderNames.ApiKey] = "PTK-DEMO";
          }

          await next();
        });

        subApp.UseCors();

        // Ocelot harus dipanggil sinkron blocking
        subApp.UseOcelot().GetAwaiter().GetResult();
      });

    return app;
  }
}
