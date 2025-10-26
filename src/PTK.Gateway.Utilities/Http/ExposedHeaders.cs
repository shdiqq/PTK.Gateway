namespace PTK.Gateway.Utilities.Http;

public static class ExposedHeaders
{
  public static readonly string[] All =
  {
    HeaderNames.RequestId,
    RateLimitHeaderNames.Limit,
    RateLimitHeaderNames.Remaining,
    RateLimitHeaderNames.Reset
  };
}
