namespace PTK.Gateway.Domain.Policies;

public static class TemplateMatcher
{
  public static bool MatchTemplate(string relPath, string template)
  {
    var p = relPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    var t = template.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (p.Length != t.Length) return false;

    for (int i = 0; i < p.Length; i++)
      if (!SegMatch(p[i], t[i])) return false;

    return true;
  }

  private static bool SegMatch(string seg, string tpl)
  {
    if (tpl.StartsWith("{") && tpl.EndsWith("}"))
    {
      var core = tpl[1..^1]; // nama[:tipe]
      var parts = core.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
      var type = parts.Length == 2 ? parts[1].ToLowerInvariant() : "any";

      return type switch
      {
        "int" => seg.All(char.IsDigit),
        "int4" => seg.Length == 4 && seg.All(char.IsDigit),
        "guid" => Guid.TryParse(seg, out _),
        _ => true // any
      };
    }
    return string.Equals(seg, tpl, StringComparison.OrdinalIgnoreCase);
  }
}
