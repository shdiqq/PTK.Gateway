using Microsoft.AspNetCore.Http;

namespace PTK.Gateway.Utilities.Routing;

public static class PathUtils
{
  public static bool IsUnderPrefix(PathString path, string prefix) =>
    path.HasValue && path.Value!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

  public static string GetRelative(PathString path, string prefix)
  {
    var p = path.Value ?? "/";
    if (!p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return p.TrimStart('/');
    var rel = p[prefix.Length..];
    return rel.TrimStart('/');
  }

  public static string RouteGroupFor(PathString path, string apiPrefix = ApiRoutes.Prefix)
  {
    var p = path.Value ?? "/";
    if (!p.StartsWith(apiPrefix, StringComparison.OrdinalIgnoreCase)) return p;

    var seg = p.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (seg.Length >= 3) return $"/{seg[0]}/{seg[1]}/{seg[2]}"; // /api/{a}/{b}
    if (seg.Length >= 2) return $"/{seg[0]}/{seg[1]}";          // /api/{a}
    return apiPrefix;
  }

  public static string NormalizeFunnelRelative(PathString path)
  {
    // relative di bawah /api/funnel atau /api/funnel/v1
    var rel = GetRelative(path, ApiRoutes.FunnelPrefix);
    if (rel.StartsWith("v1/", StringComparison.OrdinalIgnoreCase))
      rel = rel["v1/".Length..];
    return rel;
  }

  public static string NormalizeAuthRelative(PathString path)
  {
    return GetRelative(path, ApiRoutes.AuthPrefix);
  }
}
