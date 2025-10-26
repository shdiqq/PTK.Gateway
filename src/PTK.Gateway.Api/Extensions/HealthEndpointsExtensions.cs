namespace PTK.Gateway.Api.Extensions;

public static class HealthEndpointsExtensions
{
  /// <summary>
  /// Map minimal health endpoints.
  /// - GET /health       -> "ok"  (kompatibel dengan implementasi lama)
  /// - GET /health/live  -> { status = "ok" }
  /// - GET /health/ready -> { status = "ok" }
  /// </summary>
  public static IEndpointRouteBuilder MapGatewayHealthEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/health", () => Results.Ok("ok"))
       .WithName("Health")
       .WithTags("health");

    // endpoint tambahan (tidak wajib dipakai oleh probe saat ini)
    app.MapGet("/health/live", () => Results.Json(new { status = "ok" }))
       .WithName("HealthLive")
       .WithTags("health");

    app.MapGet("/health/ready", () => Results.Json(new { status = "ok" }))
       .WithName("HealthReady")
       .WithTags("health");

    return app;
  }
}
