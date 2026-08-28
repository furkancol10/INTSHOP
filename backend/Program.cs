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
    ctx.Response.Headers.Append("Permission-Policy", "geolocation=(), microphone=(), camera=()");
    await next();
});

app.UseRateLimiter();

app.MapControllers();

app.Run();
