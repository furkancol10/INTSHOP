using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/logs")]
public class LogsController : ControllerBase
{
    private readonly IDbConnection _db;
    public LogsController(IDbConnection db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Logs()
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Sadece Admin!");

        var sql = @"
            SELECT l.id, u.username, u.role, l.action, l.ip_address, l.created_at
            FROM login_logs l
            JOIN users u ON u.id = l.user_id
            ORDER BY l.created_at DESC
            LIMIT 100";
        var rows = await _db.QueryAsync(sql);
        return Ok(rows);        
    }
}