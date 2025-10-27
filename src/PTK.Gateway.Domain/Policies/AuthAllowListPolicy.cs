namespace PTK.Gateway.Domain.Policies;

public static class AuthAllowListPolicy
{
  private static readonly Dictionary<string, string[]> _allow = new(StringComparer.OrdinalIgnoreCase)
  {
    ["GET"] =
    [
      "Role/{role}/User",
      "Tes",
      "User/{id}",
      "User/{id}/Claim/{type}",
      "User/{id}/Claim",
      "User/Email/{email}"
    ],
    ["POST"] =
    [
      "Login",
      "Login/Idaman",
      "Refresh",
      "User",
      "User/{id}/Claim",
      "User/{id}/Claim/Many"
    ],
    ["PUT"] =
    [
      "User/{id}",
      "User/{id}/Password",
      "User/Email/{email}/Password",
      "User/{id}/Claim/{type}"
    ],
    ["DELETE"] =
    [
      "User/{id}",
      "User/{id}/Claim",
      "User/{id}/Claim/{type}",
      "User/Email/{email}"
    ]
  };

  public static bool IsAllowed(string method, string relPath)
  {
    if (!_allow.TryGetValue(method.ToUpperInvariant(), out var list))
    {
      return false;
    }

    foreach (var tpl in list)
    {
      if (TemplateMatcher.MatchTemplate(relPath, tpl))
      {
        return true;
      }
    }

    return false;
  }

  public static bool IsPublicEndpoint(string relPath)
  {
    return TemplateMatcher.MatchTemplate(relPath, "Login")
        || TemplateMatcher.MatchTemplate(relPath, "Login/Idaman")
        || TemplateMatcher.MatchTemplate(relPath, "Refresh");
  }
}
