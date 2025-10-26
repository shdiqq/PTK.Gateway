using System.Diagnostics;

namespace PTK.Gateway.Api.Extensions;

public static class RequestLoggingExtensions
{
  public static IApplicationBuilder UseGatewayRequestTimingLogging(this IApplicationBuilder app)
  {
    app.Use(async (ctx, next) =>
    {
      var reqId = ctx.Request.Headers[HeaderNames.RequestId].FirstOrDefault();
      if (string.IsNullOrWhiteSpace(reqId))
      {
        reqId = Guid.NewGuid().ToString("N");
        ctx.Request.Headers[HeaderNames.RequestId] = reqId;
      }
      ctx.Response.OnStarting(() =>
      {
        ctx.Response.Headers[HeaderNames.RequestId] = reqId!;
        return Task.CompletedTask;
      });

      var path = ctx.Request.Path.Value ?? "/";
      string routeGroup = path;
      if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
      {
        var seg = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (seg.Length >= 3) routeGroup = $"/api/{seg[1]}/{seg[2]}";
        else if (seg.Length >= 2) routeGroup = $"/api/{seg[1]}";
        else routeGroup = "/api";
      }
      var userSub = ctx.User?.FindFirst(AuthClaimNames.Subject)?.Value ?? "";
      var clientIdSeen = ctx.Request.Headers[HeaderNames.ClientId].ToString();

      var sw = Stopwatch.StartNew();
      try
      {
        await next();
        sw.Stop();

        var bucket = ctx.Response.StatusCode >= 500 ? "5xx" :
                     ctx.Response.StatusCode >= 400 ? "4xx" : "2xx";

        Log.Information("HTTP {Method} {Path} => {Status} ({Bucket}) in {Elapsed} ms (reqId={ReqId}, client={ClientId}, route_group={RouteGroup}, user_sub={UserSub})",
          ctx.Request.Method, path, ctx.Response.StatusCode, bucket, sw.ElapsedMilliseconds, reqId, clientIdSeen, routeGroup, userSub);
      }
      catch (Exception ex)
      {
        sw.Stop();
        Log.Error(ex, "HTTP {Method} {Path} FAILED in {Elapsed} ms (reqId={ReqId}, client={ClientId}, route_group={RouteGroup}, user_sub={UserSub})",
          ctx.Request.Method, path, sw.ElapsedMilliseconds, reqId, clientIdSeen, routeGroup, userSub);
        throw;
      }
    });

    return app;
  }
}
