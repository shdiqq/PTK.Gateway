using System.Text;

namespace PTK.Gateway.Api.Extensions;

public static class AuthExtensions
{
  public static IServiceCollection AddGatewayJwtAuth(this IServiceCollection services, JwtOptions jwt)
  {
    AuthenticationBuilder auth = services.AddAuthentication(options =>
    {
      options.DefaultAuthenticateScheme = "GatewayBearer";
      options.DefaultChallengeScheme = "GatewayBearer";
    });

    _ = auth.AddPolicyScheme("GatewayBearer", "Select JWT scheme by iss", options =>
    {
      options.ForwardDefaultSelector = ctx =>
      {
        var iss = JwtForwardingHelper.TryReadIssuerFromAuthHeader(ctx);
        if (iss is not null)
        {
          JwtSchemeOptions? matched = jwt.Schemes.FirstOrDefault(s =>
              !string.IsNullOrWhiteSpace(s.Issuer) &&
              string.Equals(s.Issuer, iss, StringComparison.OrdinalIgnoreCase));
          if (matched is not null)
          {
            return matched.Name;
          }
        }
        return jwt.Schemes.FirstOrDefault()?.Name;
      };
    });

    foreach (JwtSchemeOptions s in jwt.Schemes)
    {
      if (!string.IsNullOrWhiteSpace(s.Authority))
      {
        _ = auth.AddJwtBearer(s.Name, o =>
        {
          o.Authority = s.Authority;
          o.RequireHttpsMetadata = s.RequireHttpsMetadata;
          o.MapInboundClaims = false;
          o.SaveToken = true;
          o.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = !string.IsNullOrWhiteSpace(s.Issuer),
            ValidIssuer = s.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(s.Audience),
            ValidAudience = s.Audience,
            ClockSkew = TimeSpan.FromSeconds(s.ClockSkewSeconds),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
          };
        });
      }
      else
      {
        if (string.IsNullOrWhiteSpace(s.Secret))
        {
          throw new InvalidOperationException($"Jwt.Schemes[{s.Name}] needs either Secret or Authority.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s.Secret));
        _ = auth.AddJwtBearer(s.Name, o =>
        {
          o.RequireHttpsMetadata = false;
          o.MapInboundClaims = false;
          o.SaveToken = true;
          o.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = !string.IsNullOrWhiteSpace(s.Issuer),
            ValidIssuer = s.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(s.Audience),
            ValidAudience = s.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(s.ClockSkewSeconds),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
          };
        });
      }
    }

    _ = services.AddAuthorization();
    return services;
  }
}
