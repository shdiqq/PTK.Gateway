namespace PTK.Gateway.Api.Extensions;

public static class SerilogExtensions
{
  public static IHostBuilder UseGatewaySerilog(this IHostBuilder host, LokiOptions loki, IHostEnvironment env)
  {
    return host.UseSerilog((ctx, lc) =>
    {
      lc.MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("env", env.EnvironmentName)
        .Enrich.WithProperty("app", "ptk-gateway")
        .Enrich.WithProperty("host", Environment.MachineName)
        .WriteTo.Console(new RenderedCompactJsonFormatter());

      if (!env.IsDevelopment() && !string.IsNullOrWhiteSpace(loki.Url))
      {
        lc.WriteTo.GrafanaLoki(
          loki.Url!,
          labels: new[]
          {
            new LokiLabel { Key = "app",  Value = "ptk-gateway" },
            new LokiLabel { Key = "env",  Value = env.EnvironmentName },
            new LokiLabel { Key = "host", Value = Environment.MachineName }
          },
          textFormatter: new RenderedCompactJsonFormatter()
        );
      }
    });
  }
}
