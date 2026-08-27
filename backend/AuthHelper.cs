using Dapper;
using System.Data;
using System.Security.Cryptography;
using System.Text;

public static class AuthHelper
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static async Task<(int Id, string Role)?> GetUser(IDbConnection db, string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var hash = HashToken(token);
        var u = await db.QueryFirstOrDefaultAsync(
            @"SELECT u.id, u.role FROM sessions s
              JOIN users u ON u.id = s.user_id
              WHERE s.token_hash = @hash AND s.revoked_at IS NULL AND s.expires_at > NOW()",
            new { hash });

        return u is null ? null : ((int)u.id, (string)u.role);
    }

    public static async Task<string?> GetRole(IDbConnection db, string? token)
    {
        var user = await GetUser(db, token);
        return user?.Role;
    }
}
