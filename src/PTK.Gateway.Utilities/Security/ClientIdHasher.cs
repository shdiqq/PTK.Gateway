using System.Security.Cryptography;
using System.Text;

namespace PTK.Gateway.Utilities.Security;

public static class ClientIdHasher
{
  public static string FromIpUa(string ip, string? userAgent)
  {
    using var sha = SHA256.Create();
    return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes($"{ip}|{userAgent ?? "-"}")));
  }
}
