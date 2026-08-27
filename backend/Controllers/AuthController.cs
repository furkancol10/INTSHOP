using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Data;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly IDbConnection _db;
    public AuthController(IDbConnection db) => _db = db;

    private static readonly string[] GecerliRoller = { "Admin", "Bayi", "Kullanici" };

    // Kullanici bulunamadigi durumda da BCrypt'in calisma suresini gerceklestirmek icin
    // (M-01: zamanlama uzerinden kullanici adi sizdirmayi onler). Sabit bir string degil,
    // her uygulama baslangicinda uretilen gecerli bir bcrypt hash - deger onemli degil,
    // sadece BCrypt.Verify'in gercek bir hash'e karsi calismasi onemli.
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));

    public record RegisterReq(string username, string password, string email, string role, string? address, string? phone);
    public record LoginReq(string username, string password);
    public record SignupReq(string username, string password, string email, string? address, string? phone);

    private static bool SifreGecerliMi(string? password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= 6;

    [HttpPost("register")]
    [EnableRateLimiting("admin-write")]
    public async Task<IActionResult> Register([FromBody] RegisterReq req)
    {
        var token = Request.Headers["Authorization"].ToString();
        var actor = await AuthHelper.GetUser(_db, token);
        if (actor is null || actor.Value.Role != "Admin") return StatusCode(403, "Sadece Admin kullanıcı ekleyebilir!");

        if (!GecerliRoller.Contains(req.role))
            return BadRequest("Geçersiz rol");
        if (!SifreGecerliMi(req.password))
            return BadRequest("Şifre en az 6 karakter olmalı !");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.password);
        try
        {
            var id = await _db.ExecuteScalarAsync<int>(
                @"INSERT INTO users (username, password_hash, email, role, address, phone)
                  VALUES (@username, @hash, @email, @role, @address, @phone) RETURNING id",
                new { req.username, hash, req.email, req.role, req.address, req.phone });

            await AuditLogger.Log(_db, actor.Value.Id, "user_create", "user", id,
                new { req.username, req.role }, HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { id, req.username, req.email, req.role });
        }
        catch (Npgsql.PostgresException pex) when (pex.SqlState == "23505")
        {
            return BadRequest("Bu kullanıcı adı veya e-posta zaten kullanılıyor");
        }
        catch (Exception)
        {
            return StatusCode(500, "Kayıt sırasında bir hata oluştu");
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginReq req)
    {
        var ipOnFail = HttpContext.Connection.RemoteIpAddress?.ToString();
        var user = await _db.QueryFirstOrDefaultAsync(
            "SELECT id, password_hash, role, avatar_url FROM users WHERE username = @username",
            new { req.username });

        // Kullanici var/yok her iki durumda da BCrypt calistirilir ve ayni genel mesaj
        // donulur - hem mesaj hem zamanlama uzerinden kullanici adi sizdirmayi onler (M-01).
        var hashToVerify = user is null ? DummyPasswordHash : (string)user.password_hash;
        bool ok = BCrypt.Net.BCrypt.Verify(req.password, hashToVerify);

        if (user is null || !ok)
        {
            await _db.ExecuteAsync(
                @"INSERT INTO login_logs (user_id, attempted_username, action, ip_address)
                  VALUES (NULL, @attemptedUsername, 'login_failed', @ip)",
                new { attemptedUsername = req.username, ip = ipOnFail });
            return Unauthorized("Kullanıcı adı veya şifre hatalı");
        }

        var token = Guid.NewGuid().ToString("N");
        var tokenHash = AuthHelper.HashToken(token);
        var userId = (int)user.id;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        // Ayni kullanicinin onceki aktif oturumlarini iptal et - yeni giris eskisini dusurur.
        await _db.ExecuteAsync(
            "UPDATE sessions SET revoked_at = NOW() WHERE user_id = @userId AND revoked_at IS NULL",
            new { userId });

        await _db.ExecuteAsync(
            @"INSERT INTO sessions (user_id, token_hash, expires_at, user_agent, ip_address)
              VALUES (@userId, @tokenHash, NOW() + @lifetime, @userAgent, @ip)",
            new { userId, tokenHash, lifetime = AuthHelper.TokenLifetime, userAgent, ip });

        await _db.ExecuteAsync(
            "INSERT INTO login_logs (user_id, action, ip_address) VALUES (@userId, 'login', @ip)",
            new { userId, ip });

        return Ok(new { token, role = (string)user.role, username = req.username, avatar_url = (string?)user.avatar_url });
    }

    [HttpPost("signup")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Signup([FromBody] SignupReq req)
    {
        if (string.IsNullOrWhiteSpace(req.username))
            return BadRequest("Kullanıcı adı zorunlu !");
        if (!SifreGecerliMi(req.password))
            return BadRequest("Şifre en az 6 karakter olmalı !");
        if (string.IsNullOrWhiteSpace(req.email) || !EpostaGecerliMi(req.email))
            return BadRequest("Geçerli bir e-posta adresi giriniz!");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.password);
        try
        {
            var id = await _db.ExecuteScalarAsync<int>(
                @"INSERT INTO users(username, password_hash, role, email, address, phone)
                    VALUES (@username, @hash, 'Kullanici', @email, @address, @phone)
                    RETURNING id",
                new { req.username, hash, req.email, req.address, req.phone });
            return Ok(new { id, req.username });
        }
        catch (Npgsql.PostgresException pex) when (pex.SqlState == "23505")
        {
            return BadRequest("Bu kullanıcı adı veya e-posta zaten kullanılıyor");
        }
        catch (Exception)
        {
            return StatusCode(500, "Kayıt sırasında bir hata oluştu");
        }
    }

    private static bool EpostaGecerliMi(string eposta)
    {
        try
        {
            var adres = new System.Net.Mail.MailAddress(eposta.Trim());
            return adres.Address == eposta.Trim();
        }
        catch
        {
            return false;
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await AuthHelper.GetUser(_db, token);
        if (user is null) return Unauthorized();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _db.ExecuteAsync(
            "INSERT INTO login_logs (user_id, action, ip_address) VALUES (@userId, 'logout', @ip)",
            new { userId = user.Value.Id, ip });

        var tokenHash = AuthHelper.HashToken(token);
        await _db.ExecuteAsync(
            "UPDATE sessions SET revoked_at = NOW() WHERE token_hash = @tokenHash AND revoked_at IS NULL",
            new { tokenHash });

        return Ok();
    }
    
}