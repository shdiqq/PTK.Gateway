using PTK.Gateway.Utilities.Security;

using Xunit;

namespace PTK.Gateway.Domain.Tests;

public class ClientIdHasherTests
{
  [Fact]
  public void FromIpUa_Deterministic()
  {
    var a = ClientIdHasher.FromIpUa("1.2.3.4", "Chrome");
    var b = ClientIdHasher.FromIpUa("1.2.3.4", "Chrome");
    Assert.Equal(a, b);
  }

  [Fact]
  public void FromIpUa_ChangesWithInput()
  {
    var a = ClientIdHasher.FromIpUa("1.2.3.4", "Chrome");
    var b = ClientIdHasher.FromIpUa("1.2.3.5", "Chrome");
    var c = ClientIdHasher.FromIpUa("1.2.3.4", "Firefox");

    Assert.NotEqual(a, b);
    Assert.NotEqual(a, c);
  }
}
