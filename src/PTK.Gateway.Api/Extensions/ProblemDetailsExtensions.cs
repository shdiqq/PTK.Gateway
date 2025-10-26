using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PTK.Gateway.Api.Extensions;

public static class ProblemDetailsExtensions
{
  public static IResult CreateProblem(
    HttpContext ctx, int status, string title,
    string? detail = null, string? type = null, string? instance = null)
  {
    var problem = new ProblemDetails
    {
      Status = status,
      Title = title,
      Detail = detail,
      Type = type,
      Instance = instance ?? ctx.Request.Path
    };

    // extra info untuk observability
    problem.Extensions["requestId"] = ctx.Response.Headers[HeaderNames.RequestId].ToString();
    problem.Extensions["traceId"] = ctx.TraceIdentifier;

    return Results.Json(problem, statusCode: status, contentType: "application/problem+json");
  }

  // Memudahkan dipakai di middleware (yang tidak bisa return IResult langsung)
  public static Task WriteProblemAsync(
    HttpContext ctx, int status, string title,
    string? detail = null, string? type = null, string? instance = null)
    => CreateProblem(ctx, status, title, detail, type, instance).ExecuteAsync(ctx);

  // Global exception handler (dipanggil di Program.cs)
  public static void UseGlobalExceptionProblem(this IApplicationBuilder app, bool showDetails)
  {
    app.UseExceptionHandler(errorApp =>
    {
      errorApp.Run(async ctx =>
      {
        var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
        var detail = showDetails ? ex?.ToString() : "An unexpected error occurred.";
        await WriteProblemAsync(ctx, StatusCodes.Status500InternalServerError, "Internal Server Error", detail);
      });
    });
  }
}
