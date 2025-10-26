namespace PTK.Gateway.Api.Extensions;

public static class CorsExtensions
{
  public static IServiceCollection AddGatewayCors(this IServiceCollection services, CorsOptions cors)
  {
    var allowed = cors.AllowedOrigins ?? Array.Empty<string>();

    services.AddCors(o =>
    {
      o.AddDefaultPolicy(p =>
      {
        if (allowed.Length > 0)
          p.WithOrigins(allowed).AllowAnyHeader().AllowAnyMethod();
        else
          p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); // fallback dev

        p.WithExposedHeaders(ExposedHeaders.All);
      });
    });

    return services;
  }
}
