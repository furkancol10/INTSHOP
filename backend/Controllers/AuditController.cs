using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly IDbConnection _db;
    public AuditController(IDbConnection db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Sadece Admin!");

        var sql = @"
            SELECT a.id, u.username AS actor, a.action, a.target_type, a.target_id,
                   a.details, a.ip_address, a.created_at
            FROM audit_log a
            LEFT JOIN users u ON u.id = a.actor_id
            ORDER BY a.created_at DESC
            LIMIT 100";
        var rows = await _db.QueryAsync(sql);
        return Ok(rows);
    }
}

public static class AuditLogger
{
    public static Task Log(IDbConnection db, int actorId, string action, string? targetType,
        int? targetId, object? details, string? ipAddress, IDbTransaction? tx = null)
    {
        return db.ExecuteAsync(
            @"INSERT INTO audit_log (actor_id, action, target_type, target_id, details, ip_address)
              VALUES (@actorId, @action, @targetType, @targetId, @details::jsonb, @ipAddress)",
            new
            {
                actorId,
                action,
                targetType,
                targetId,
                details = details is null ? null : System.Text.Json.JsonSerializer.Serialize(details),
                ipAddress
            },
            tx);
    }
}
