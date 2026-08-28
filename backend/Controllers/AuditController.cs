using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
public class AuditController : ControllerBase
{
    private readonly IDbConnection _db;
    public AuditController(IDbConnection db) => _db = db;

    [HttpGet("api/audit")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? action = null,
        [FromQuery] string? entity = null,
        [FromQuery] int? actorId = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Sadece Admin");

        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(offset, 0);

        var sql = @"
            SELECT a.id, a.actor_id, u.username AS actor_username, a.actor_role,
                   a.action, a.entity, a.entity_id, a.old_value, a.new_value,
                   a.ip_address, a.created_at
            FROM audit_log a
            LEFT JOIN users u ON u.id = a.actor_id
            WHERE (@action IS NULL OR a.action LIKE @action)
              AND (@entity IS NULL OR a.entity = @entity)
              AND (@actorId IS NULL OR a.actor_id = @actorId)
            ORDER BY a.created_at DESC
            LIMIT @limit OFFSET @offset";

        var rows = await _db.QueryAsync(sql, new { action, entity, actorId, limit, offset });
        return Ok(rows);
    }
}
