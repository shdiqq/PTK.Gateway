using Microsoft.Extensions.Options;

using Ocelot.Middleware;

namespace PTK.Gateway.Api.Extensions;

public static class ApiBranchExtensions
{
  public static IApplicationBuilder UseGatewayApiBranch(
    this IApplicationBuilder app,
    SecurityOptions security)
  {
    _ = app.MapWhen(
      ctx => PathUtils.IsUnderPrefix(ctx.Request.Path, ApiRoutes.Prefix),
      subApp =>
      {
        _ = subApp.Use(async (ctx, next) =>
        {
          // ambil API keys dari konfigurasi
          Dictionary<string, string> apiKeys = ctx.RequestServices.GetRequiredService<IOptions<ApiKeysOptions>>().Value.Values;

          // bersihkan header sensitif jika ada
          if (ctx.Request.Headers.ContainsKey(HeaderNames.ApiKey))
          {
            _ = ctx.Request.Headers.Remove(HeaderNames.ApiKey);
          }

          if (ctx.Request.Headers.ContainsKey(HeaderNames.ClientId))
          {
            _ = ctx.Request.Headers.Remove(HeaderNames.ClientId);
          }

          if (ctx.Request.Headers.ContainsKey(HeaderNames.UserSub))
          {
            _ = ctx.Request.Headers.Remove(HeaderNames.UserSub);
          }

          if (ctx.Request.Headers.ContainsKey(HeaderNames.UserRole))
          {
            _ = ctx.Request.Headers.Remove(HeaderNames.UserRole);
          }

          // tentukan client-id
          string? clientId = null;
          if (ctx.User?.Identity?.IsAuthenticated == true)
          {
            clientId = ctx.User.FindFirst(AuthClaimNames.Subject)?.Value
                       ?? ctx.User.FindFirst(AuthClaimNames.ClientId)?.Value;
          }

          if (string.IsNullOrWhiteSpace(clientId))
          {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var ua = ctx.Request.Headers["User-Agent"].FirstOrDefault();
            clientId = ClientIdHasher.FromIpUa(ip, ua);
          }
          ctx.Request.Headers[HeaderNames.ClientId] = clientId!;

          // propagate identitas ke layanan internal saja
          if (ctx.User?.Identity?.IsAuthenticated == true)
          {
            var sub = ctx.User.FindFirst(AuthClaimNames.Subject)?.Value;
            var role = ctx.User.FindFirst(AuthClaimNames.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(sub))
            {
              ctx.Request.Headers[HeaderNames.UserSub] = sub;
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
              ctx.Request.Headers[HeaderNames.UserRole] = role;
            }
          }

          var path = ctx.Request.Path.Value ?? "/";

          // ---- FUNNEL (internal)
          if (PathUtils.IsUnderPrefix(ctx.Request.Path, ApiRoutes.FunnelPrefix))
          {
            if (!(ctx.User?.Identity?.IsAuthenticated ?? false))
            {
              await ProblemDetailsExtensions.WriteProblemAsync(ctx,
                StatusCodes.Status401Unauthorized, "Unauthorized",
                "Authentication is required to access this resource.");
              return;
            }

            var rel = PathUtils.NormalizeFunnelRelative(ctx.Request.Path);
            if (!FunnelAllowListPolicy.IsAllowed(ctx.Request.Method, rel))
            {
              var status = security.HideForbidden ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
              var title = security.HideForbidden ? "Not Found" : "Forbidden";
              var detail = security.HideForbidden
                           ? "The requested endpoint is not available."
                           : "Endpoint not allowed via gateway policy.";
              await ProblemDetailsExtensions.WriteProblemAsync(ctx, status, title, detail);
              return;
            }

            // buang header Authorization dan identitas pengguna
            if (ctx.Request.Headers.ContainsKey(HeaderNames.Authorization))
            {
              _ = ctx.Request.Headers.Remove(HeaderNames.Authorization);
            }

            if (ctx.Request.Headers.ContainsKey(HeaderNames.UserSub))
            {
              _ = ctx.Request.Headers.Remove(HeaderNames.UserSub);
            }

            if (ctx.Request.Headers.ContainsKey(HeaderNames.UserRole))
            {
              _ = ctx.Request.Headers.Remove(HeaderNames.UserRole);
            }

            // ganti client-id dengan anonimus berbasis IP+UA
            var ip2 = ctx.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var ua2 = ctx.Request.Headers.UserAgent.FirstOrDefault();
            var anon = ClientIdHasher.FromIpUa(ip2, ua2);
            ctx.Request.Headers[HeaderNames.ClientId] = anon;

            // pasang API key Funnel
            if (apiKeys.TryGetValue("funnel", out var funnelKey) && !string.IsNullOrWhiteSpace(funnelKey))
            {
              ctx.Request.Headers[HeaderNames.ApiKey] = funnelKey;
            }
          }

          // ---- AUTH (publik & internal)
          if (PathUtils.IsUnderPrefix(ctx.Request.Path, ApiRoutes.AuthPrefix))
          {
            var rel = PathUtils.NormalizeAuthRelative(ctx.Request.Path);

            if (!AuthAllowListPolicy.IsAllowed(ctx.Request.Method, rel))
            {
              var status = security.HideForbidden ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
              var title = security.HideForbidden ? "Not Found" : "Forbidden";
              var detail = security.HideForbidden
                           ? "The requested endpoint is not available."
                           : "Endpoint not allowed via gateway policy.";
              await ProblemDetailsExtensions.WriteProblemAsync(ctx, status, title, detail);
              return;
            }

            var isPublic = AuthAllowListPolicy.IsPublicEndpoint(rel);
            if (!isPublic && !(ctx.User?.Identity?.IsAuthenticated ?? false))
            {
              await ProblemDetailsExtensions.WriteProblemAsync(ctx,
                StatusCodes.Status401Unauthorized, "Unauthorized",
                "Authentication is required to access this resource.");
              return;
            }

            // Authorization dibiarkan lewat; buang header identitas “buatan gateway”
            if (ctx.Request.Headers.ContainsKey(HeaderNames.UserSub))
            {
              _ = ctx.Request.Headers.Remove(HeaderNames.UserSub);
            }

            if (ctx.Request.Headers.ContainsKey(HeaderNames.UserRole))
            {
              _ = ctx.Request.Headers.Remove(HeaderNames.UserRole);
            }
          }

          // ---- BOS (internal, perlu JWT)
          if (PathUtils.IsUnderPrefix(ctx.Request.Path, ApiRoutes.BosPrefix))
          {
            if (!(ctx.User?.Identity?.IsAuthenticated ?? false))
            {
              await ProblemDetailsExtensions.WriteProblemAsync(ctx,
                StatusCodes.Status401Unauthorized, "Unauthorized",
                "Authentication is required to access this resource.");
              return;
            }

            if (apiKeys.TryGetValue("bos", out var bosKey) && !string.IsNullOrWhiteSpace(bosKey))
            {
              ctx.Request.Headers[HeaderNames.ApiKey] = bosKey;
            }
          }

          // ---- CORE (internal, perlu JWT)
          if (PathUtils.IsUnderPrefix(ctx.Request.Path, ApiRoutes.CorePrefix))
          {
            if (!(ctx.User?.Identity?.IsAuthenticated ?? false))
            {
              await ProblemDetailsExtensions.WriteProblemAsync(ctx,
                StatusCodes.Status401Unauthorized, "Unauthorized",
                "Authentication is required to access this resource.");
              return;
            }

            if (apiKeys.TryGetValue("core", out var coreKey) && !string.IsNullOrWhiteSpace(coreKey))
            {
              ctx.Request.Headers[HeaderNames.ApiKey] = coreKey;
            }
          }

          // ---- ECHO (publik, demo)
          if (path.StartsWith("/api/echo", StringComparison.OrdinalIgnoreCase))
          {
            ctx.Request.Headers[HeaderNames.ApiKey] = "PTK-DEMO";
          }

          await next();
        });

        _ = subApp.UseCors();
        _ = subApp.UseOcelot().GetAwaiter().GetResult();
      });

    return app;
  }
}
