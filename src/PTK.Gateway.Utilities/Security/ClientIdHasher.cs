using System.Security.Cryptography;
using System.Text;

namespace PTK.Gateway.Utilities.Security;

public static class ClientIdHasher
{
  public static string FromIpUa(string ip, string? userAgent)
  {
    var payload = Encoding.UTF8.GetBytes($"{ip}|{userAgent ?? "-"}");
    var hash = SHA256.HashData(payload);
    return Convert.ToHexString(hash);
  }
}
