using PTK.Gateway.Domain.Policies;

using Xunit;

namespace PTK.Gateway.Domain.Tests;

public class AuthAllowListPolicyTests
{
  [Theory]
  [InlineData("POST", "Login", true)]
  [InlineData("POST", "Refresh", true)]
  [InlineData("GET", "User/123", true)]
  [InlineData("DELETE", "User/123/Claim/role", true)]
  [InlineData("PATCH", "User/123", false)] // PATCH tidak diizinkan
  [InlineData("GET", "Unknown", false)]
  public void IsAllowed_Cases(string method, string rel, bool expected)
    => Assert.Equal(expected, AuthAllowListPolicy.IsAllowed(method, rel));

  [Theory]
  [InlineData("Login", true)]
  [InlineData("Login/Idaman", true)]
  [InlineData("Refresh", true)]
  [InlineData("User/1", false)]
  public void IsPublicEndpoint_Cases(string rel, bool expected)
    => Assert.Equal(expected, AuthAllowListPolicy.IsPublicEndpoint(rel));
}
