using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api")]
public class UsersController : ControllerBase
{
    private readonly IDbConnection _db;
    public UsersController(IDbConnection db) => _db = db;

    [HttpGet("dealers")]
    public async Task<IActionResult> GetDealers()
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Sadece Admin!");

        var rows = await _db.QueryAsync(
            "SELECT id, username, address, phone FROM users WHERE role = 'Bayi' ORDER BY username"
        );
        return Ok(rows);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Sadece Admin!");

        var rows = await _db.QueryAsync(
            "SELECT id, username, role FROM users ORDER BY id"
        );
        return Ok(rows);
    }
}