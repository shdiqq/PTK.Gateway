namespace PTK.Gateway.Api.Extensions;

public static class AppInfoHeadersExtensions
{
  public static IApplicationBuilder UseAppInfoHeaders(this IApplicationBuilder app)
  {
    app.Use(async (ctx, next) =>
    {
      ctx.Response.OnStarting(() =>
      {
        ctx.Response.Headers[HeaderNames.AppName] = AppInfo.Name;
        ctx.Response.Headers[HeaderNames.AppVersion] = AppInfo.Version;
        return Task.CompletedTask;
      });
      await next();
    });
    return app;
  }
}
