using System.Text;

namespace PTK.Gateway.Api.Extensions;

public static class AuthExtensions
{
  public static IServiceCollection AddGatewayJwtAuth(this IServiceCollection services, JwtOptions jwt)
  {
    var keyBytes = Encoding.UTF8.GetBytes(jwt.Secret);

    services
      .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(opt =>
      {
        opt.RequireHttpsMetadata = false; // dev only
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
          ValidateIssuer = true,
          ValidIssuer = jwt.Issuer,
          ValidateAudience = true,
          ValidAudience = jwt.Audience,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.Zero,
          NameClaimType = JwtRegisteredClaimNames.Sub,
          RoleClaimType = "role"
        };
      });

    services.AddAuthorization();
    return services;
  }
}
