using Npgsql;
using System.Data;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddDefaultPolicy(p=>
p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(builder.Configuration.GetConnectionString("Default")));

// Giris/kayit denemelerinde brute-force'u zorlastirmak icin IP basina hiz siniri
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseCors();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "no-referrer");
    ctx.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    // HSTS: yalniz HTTPS'te gecerli (HTTP'de tarayici yok sayar), TLS eklenince devreye girer.
    ctx.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    // API sadece JSON doner; default-src 'none' JSON istemciyi etkilemez, yanlislikla
    // HTML/hata sayfasi dönerse kaynak yuklemesini ve iframe'lenmeyi engeller (defense-in-depth).
    ctx.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'");
    await next();
});

app.UseRateLimiter();

app.MapControllers();

app.Run();
