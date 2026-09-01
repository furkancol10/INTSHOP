using Npgsql;
using System.Data;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Dapper;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

builder.Services.AddControllers();
var allowedOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// Render/Heroku gibi platformlar baglanti dizesini URI formatinda verir
// (postgresql://kullanici:parola@host:5432/db). Npgsql key-value formati bekledigi
// icin URI gelirse cevirilir; zaten key-value ise oldugu gibi kullanilir.
static string PostgresBaglantiDizesi(string? ham)
{
    if (string.IsNullOrWhiteSpace(ham)) return ham ?? "";
    if (!ham.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !ham.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return ham;

    var uri = new Uri(ham);
    var kimlik = uri.UserInfo.Split(':', 2);
    var csb = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.Trim('/'),
        Username = Uri.UnescapeDataString(kimlik[0]),
        Password = kimlik.Length > 1 ? Uri.UnescapeDataString(kimlik[1]) : ""
    };
    // Render ic aglarda TLS istemez, dis baglantilarda zorunlu kilar; Prefer ikisini de karsilar.
    csb["SSL Mode"] = "Prefer";
    csb["Trust Server Certificate"] = true;
    return csb.ConnectionString;
}

var postgresBaglanti = PostgresBaglantiDizesi(builder.Configuration.GetConnectionString("Default"));
builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(postgresBaglanti));

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

// Zorunlu parola degisimi server-side zorlanir: must_change_password=true olan
// bir token, sifresini degistirene kadar yalnizca asagidaki uclara erisebilir.
// Aksi halde frontend modal'i atlanip API dogrudan cagirilabiliyordu.
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    bool izinli = path.StartsWith("/api/change-password")
               || path.StartsWith("/api/profile")
               || path.StartsWith("/api/logout")
               || path.StartsWith("/api/login");
    if (!izinli)
    {
        var token = ctx.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token))
        {
            var db = ctx.RequestServices.GetRequiredService<IDbConnection>();
            var mustChange = await db.ExecuteScalarAsync<bool?>(
                "SELECT must_change_password FROM users WHERE token = @token",
                new { token = AuthHelper.TokenHash(token) });
            if (mustChange == true)
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsync("Once varsayilan sifrenizi degistirmelisiniz.");
                return;
            }
        }
    }
    await next();
});

app.MapControllers();

app.Run();
