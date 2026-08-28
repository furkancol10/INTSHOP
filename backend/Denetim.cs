using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Http;

public static class Denetim
{
    public static async Task Yaz(
        IDbConnection db,
        HttpContext ctx,
        int actorId,
        string actorRole,
        string action,
        string entity,
        int? entityId,
        object? oldValue = null,
        object? newValue = null)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString();

        await db.ExecuteAsync(
            @"INSERT INTO audit_log (actor_id, actor_role, action, entity, entity_id, old_value, new_value, ip_address)
              VALUES (@actorId, @actorRole, @action, @entity, @entityId, @oldValue::jsonb, @newValue::jsonb, @ipAddress)",
            new
            {
                actorId,
                actorRole,
                action,
                entity,
                entityId,
                oldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
                newValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
                ipAddress = ip
            });
    }
}
