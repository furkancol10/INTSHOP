using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api")]
public class ProfileController : ControllerBase
{
    private readonly IDbConnection _db;
    public ProfileController(IDbConnection db) => _db = db;

    private Task<(int id, string role)?> GetUser(string? token)
        => AuthHelper.GetUser(_db, token);

    public record ProfileUpdate(string? address, string? phone, string? avatar_url);

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await GetUser(token);
        if (user is null) return Unauthorized("Giriş gerekli");

        var data = await _db.QueryFirstOrDefaultAsync(
            "SELECT id, username, role, address, phone, avatar_url FROM users WHERE id = @id",
            new { id = user.Value.id });
        return Ok(data);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdate p)
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await GetUser(token);
        if (user is null) return Unauthorized("Giriş gerekli");
        if (!UrlValidator.GuvenliResimUrlMi(p.avatar_url))
            return BadRequest("Geçersiz avatar adresi (yalnızca https:// veya / ile başlayan yollar kabul edilir)");

        await _db.ExecuteAsync(
            "UPDATE users SET address = @address, phone = @phone, avatar_url = @avatar_url WHERE id = @id",
            new { p.address, p.phone, p.avatar_url, id = user.Value.id });
        return Ok();
    }

    public record PasswordChangeReq(string currentPassword, string newPassword);

    [HttpPut("profile/password")]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeReq req)
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await GetUser(token);
        if (user is null) return Unauthorized("Giriş gerekli");

        if (string.IsNullOrWhiteSpace(req.newPassword) || req.newPassword.Length < 6)
            return BadRequest("Yeni şifre en az 6 karakter olmalı !");

        var currentHash = await _db.ExecuteScalarAsync<string?>(
            "SELECT password_hash FROM users WHERE id = @id", new { id = user.Value.id });

        if (currentHash is null || !BCrypt.Net.BCrypt.Verify(req.currentPassword, currentHash))
            return BadRequest("Mevcut şifre hatalı");

        var newHash = BCrypt.Net.BCrypt.HashPassword(req.newPassword);
        await _db.ExecuteAsync(
            "UPDATE users SET password_hash = @newHash WHERE id = @id",
            new { newHash, id = user.Value.id });

        // Sifre degisince tum oturumlar iptal olur - calinmis bir token da bu andan
        // itibaren gecersiz kalir. Kullanici yeniden giris yapmak zorunda kalir.
        await _db.ExecuteAsync(
            "UPDATE sessions SET revoked_at = NOW() WHERE user_id = @id AND revoked_at IS NULL",
            new { id = user.Value.id });

        return Ok(new { degistirildi = true });
    }
}