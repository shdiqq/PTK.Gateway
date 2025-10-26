namespace PTK.Gateway.Domain.Policies;

public static class FunnelAllowListPolicy
{
  private static readonly Dictionary<string, string[]> Allow = new(StringComparer.OrdinalIgnoreCase)
  {
    ["GET"] = new[]
    {
      "BusinessSegmentation",
      "BusinessSegmentation/{id:int}",
      "FileReader/data/{id:int}",
      "FileReader/{id:int}",
      "MasterProject/{id:int}",
      "Progress",
      "Progress/{id:int}",
      "TransactionRevenue/{id:int}",
      "TransactionRevenue/DetailMasterByYear/{id:int}/{year:int4}",
      "TransactionRKAP/{id:int}",
      "TransactionRKAP/DetailMasterByYear/{id:int}/{year:int4}",
      "User",
      "User/{id:int}"
    },
    ["POST"] = new[]
    {
      "Dashboard/Contract",
      "Dashboard/FunnelOpportunities",
      "Dashboard/Management",
      "BusinessSegmentation/DataTable",
      "MasterProject/DataTable",
      "Progress/DataTable",
      "User/DataTable",
      "BusinessSegmentation",
      "MasterProject",
      "MasterProject/CreateMasterByYear",
      "MasterProject/Customer",
      "MasterProject/ExportAll",
      "Progress",
      "Report/Captive",
      "Report/CaptiveTx",
      "Report/Downstream",
      "Report/Segmen",
      "Report/Remarks",
      "TransactionRevenue",
      "TransactionRevenue/CreateMasterByYear",
      "TransactionRKAP",
      "TransactionRKAP/CreateMasterByYear",
      "User",
      "User/Login",
      "User/LoginIdaman/{Email}/{FullName}"
    },
    ["PUT"] = new[]
    {
      "BusinessSegmentation/{id:int}",
      "BusinessSegmentation/Delete",
      "MasterProject/{id:int}",
      "MasterProject/Delete",
      "Progress/{id:int}",
      "Progress/Delete",
      "TransactionRevenue/{id:int}",
      "TransactionRevenue/Delete",
      "TransactionRKAP/{id:int}",
      "TransactionRKAP/Delete",
      "User/{id:int}",
      "User/Delete",
      "User/UpdatePassword/{id:int}"
    },
    ["DELETE"] = Array.Empty<string>()
  };

  public static bool IsAllowed(string method, string relPath)
  {
    if (!Allow.TryGetValue(method.ToUpperInvariant(), out var list)) return false;
    foreach (var tpl in list)
      if (TemplateMatcher.MatchTemplate(relPath, tpl)) return true;
    return false;
  }
}
