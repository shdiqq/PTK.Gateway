using PTK.Gateway.Domain.Policies;

using Xunit;

namespace PTK.Gateway.Domain.Tests;

public class FunnelAllowListPolicyTests
{
  [Theory]
  [InlineData("GET", "User/123", true)]
  [InlineData("GET", "Unknown", false)]
  [InlineData("POST", "User/Login", true)]
  [InlineData("DELETE", "User/1", false)] // DELETE tidak diizinkan
  public void IsAllowed_Cases(string method, string rel, bool expected)
  {
    Assert.Equal(expected, FunnelAllowListPolicy.IsAllowed(method, rel));
  }

  [Fact]
  public void IsAllowed_CaseInsensitiveMethod()
  {
    Assert.True(FunnelAllowListPolicy.IsAllowed("get", "User/123"));
  }
}
