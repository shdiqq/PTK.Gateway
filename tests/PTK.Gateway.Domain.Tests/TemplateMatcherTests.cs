using PTK.Gateway.Domain.Policies;

using Xunit;

namespace PTK.Gateway.Domain.Tests;

public class TemplateMatcherTests
{
  [Theory]
  [InlineData("User/123", "User/{id:int}", true)]
  [InlineData("User/abc", "User/{id:int}", false)]
  [InlineData("BusinessSegmentation", "BusinessSegmentation", true)]
  [InlineData("businesssegmentation", "BusinessSegmentation", true)] // case-insensitive
  [InlineData("TransactionRevenue/DetailMasterByYear/42/2024", "TransactionRevenue/DetailMasterByYear/{id:int}/{year:int4}", true)]
  [InlineData("TransactionRevenue/DetailMasterByYear/42/24", "TransactionRevenue/DetailMasterByYear/{id:int}/{year:int4}", false)]
  public void MatchTemplate_Basic(string rel, string tpl, bool expected)
  {
    Assert.Equal(expected, TemplateMatcher.MatchTemplate(rel, tpl));
  }

  [Fact]
  public void MatchTemplate_Guid()
  {
    var g = Guid.NewGuid().ToString();
    Assert.True(TemplateMatcher.MatchTemplate($"Foo/{g}", "Foo/{id:guid}"));
    Assert.False(TemplateMatcher.MatchTemplate("Foo/not-a-guid", "Foo/{id:guid}"));
  }
}
