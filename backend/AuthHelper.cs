using Dapper;
using System.Data;

public static class AuthHelper
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    // Token DB'de duz metin tutulmaz: gelen ham token SHA-256 ile hash'lenip
    // saklanan hash ile eslestirilir. Boylece DB dump'i aktif oturumlari acmaz.
    public static string TokenHash(string raw)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static async Task<(int Id, string Role)?> GetUser(IDbConnection db, string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var u = await db.QueryFirstOrDefaultAsync(
            "SELECT id, role, token_issued_at FROM users WHERE token = @token",
            new { token = TokenHash(token) });
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
