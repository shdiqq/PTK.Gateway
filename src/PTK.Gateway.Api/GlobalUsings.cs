// ASP.NET Core
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

// Auth/JWT (digunakan di Program & AuthExtensions)
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.IdentityModel.Tokens;
global using System.IdentityModel.Tokens.Jwt;

// Serilog (digunakan di SerilogExtensions & logging middleware)
global using Serilog;
global using Serilog.Formatting.Compact;
global using Serilog.Sinks.Grafana.Loki;

// Opsi & util internal
global using PTK.Gateway.Domain.Options;
global using PTK.Gateway.Domain.Policies;
global using PTK.Gateway.Utilities.Security;
global using PTK.Gateway.Utilities.Http;
