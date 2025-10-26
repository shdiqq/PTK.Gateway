namespace PTK.Gateway.Utilities.Security;

public static class AuthClaimNames
{
  // Standar JWT
  public const string Subject = "sub";        // JwtRegisteredClaimNames.Sub
  public const string Role = "role";       // custom string role yg kamu pakai
  public const string ClientId = "client_id";  // fallback yg kamu pakai di API
}
