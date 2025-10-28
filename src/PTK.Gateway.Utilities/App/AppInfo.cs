using System.Reflection;

namespace PTK.Gateway.Utilities.App;

public static class AppInfo
{
  // Ganti kalau mau sumber nama dari config
  public static string Name => "PTK.GATEWAY";

  private static string? _version;
  public static string Version => _version ??= GetInformationalVersion();

  private static string GetInformationalVersion()
  {
    var entry = Assembly.GetEntryAssembly();
    var info = entry?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    if (!string.IsNullOrWhiteSpace(info)) return info!;
    var ver = entry?.GetName().Version?.ToString();
    return ver ?? "1.0.0";
  }
}
