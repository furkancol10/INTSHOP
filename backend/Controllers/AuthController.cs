using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Data;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly IDbConnection _db;
    public AuthController(IDbConnection db) => _db = db;

    public record RegisterReq(string username, string password, string email, string role, string? address, string? phone);
    public record LoginReq(string username, string password);
    public record SignupReq(string username, string password, string email, string? address, string? phone);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterReq req)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Sadece Admin kullanıcı ekleyebilir!");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.password);
        try
        {
            var id = await _db.ExecuteScalarAsync<int>(
                @"INSERT INTO users (username, password_hash, email, role, address, phone)
                  VALUES (@username, @hash, @email, @role, @address, @phone) RETURNING id",
                new { req.username, hash, req.email, req.role, req.address, req.phone });
            return Ok(new { id, req.username, req.email, req.role });
        }
        catch
        {
            return BadRequest("Bu kullanıcı adı zaten var");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginReq req)
    {
        var user = await _db.QueryFirstOrDefaultAsync(
            "SELECT id, password_hash, role, avatar_url FROM users WHERE username = @username",
            new { req.username });

        if (user is null)
            return Unauthorized("Kullanıcı bulunamadı");

        bool ok = BCrypt.Net.BCrypt.Verify(req.password, (string)user.password_hash);
        if (!ok)
            return Unauthorized("Şifre hatalı");

        var token = Guid.NewGuid().ToString("N");
        await _db.ExecuteAsync(
            "UPDATE users SET token = @token WHERE id = @id",
            new { token, id = (int)user.id });

        await _db.ExecuteAsync(
            "INSERT INTO login_logs (user_id, action) VALUES (@userId, 'login')",
            new { userId = (int)user.id });

        return Ok(new { token, role = (string)user.role, username = req.username, avatar_url = (string?)user.avatar_url });
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupReq req)
    {
        if (string.IsNullOrWhiteSpace(req.username) || string.IsNullOrWhiteSpace(req.password))
            return BadRequest("Kullanıcı adı ve şifre zorunlu !");
        if (req.password.Length < 6)
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

        var userId = await _db.ExecuteScalarAsync<int?>(
            "SELECT id FROM users WHERE token = @token", new { token });
        if (userId is null) return Unauthorized();

        await _db.ExecuteAsync(
            "INSERT INTO login_logs (user_id, action) VALUES (@userId, 'logout')",
            new { userId = userId.Value });

        await _db.ExecuteAsync(
            "UPDATE users SET token = NULL WHERE id = @userId",
            new { userId = userId.Value });

        return Ok();
    }
    
}