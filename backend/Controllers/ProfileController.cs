using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api")]
public class ProfileController : ControllerBase
{
    private readonly IDbConnection _db;
    public ProfileController(IDbConnection db) => _db = db;

    private async Task<(int id, string role)?> GetUser(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        var u = await _db.QueryFirstOrDefaultAsync(
            "SELECT id, role FROM users WHERE token = @token", new { token });
        if (u is null) return null;
        return ((int)u.id, (string)u.role);
    }

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

        await _db.ExecuteAsync(
            "UPDATE users SET address = @address, phone = @phone, avatar_url = @avatar_url WHERE id = @id",
            new { p.address, p.phone, p.avatar_url, id = user.Value.id });
        return Ok();
    }
}