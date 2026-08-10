using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly IDbConnection _db;
    public AuthController(IDbConnection db) => _db = db;

    public record RegisterReq(string username, string password, string role, string? address, string? phone);
    public record LoginReq(string username, string password);

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
                @"INSERT INTO users (username, password_hash, role, address, phone)
                  VALUES (@username, @hash, @role, @address, @phone) RETURNING id",
                new { req.username, hash, req.role, req.address, req.phone });
            return Ok(new { id, req.username, req.role });
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

        return Ok(new { token, role = (string)user.role, username = req.username, avatar_url = (string?)user.avatar_url});
    } 
}