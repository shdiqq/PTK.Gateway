namespace PTK.Gateway.Utilities.Http;

public static class HeaderNames
{
  public const string RequestId = "X-Request-Id";
  public const string ApiKey = "X-API-KEY";
  public const string ClientId = "X-Client-Id";
  public const string UserSub = "X-User-Sub";
  public const string UserRole = "X-User-Role";
}

public static class RateLimitHeaderNames
{
  public const string Limit = "X-Rate-Limit-Limit";
  public const string Remaining = "X-Rate-Limit-Remaining";
  public const string Reset = "X-Rate-Limit-Reset";
}
