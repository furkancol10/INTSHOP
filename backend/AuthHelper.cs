using Dapper;
using System.Data;

public static class AuthHelper
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    public static async Task<(int Id, string Role)?> GetUser(IDbConnection db, string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var u = await db.QueryFirstOrDefaultAsync(
            "SELECT id, role, token_issued_at FROM users WHERE token = @token", new { token });
        if (u is null) return null;

        DateTime? issuedAt = u.token_issued_at;
        if (issuedAt is null || DateTime.UtcNow - issuedAt.Value > TokenLifetime)
            return null;

        return ((int)u.id, (string)u.role);
    }

    public static async Task<string?> GetRole(IDbConnection db, string? token)
    {
        var user = await GetUser(db, token);
        return user?.Role;
    }
}
