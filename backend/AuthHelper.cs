using Dapper;
using System.Data;

public static class AuthHelper
{
    public static async Task<string?> GetRole(IDbConnection db, string? token)
    {
        if(string.IsNullOrEmpty(token)) return null;
        return await db.QueryFirstOrDefaultAsync<string?>(
            "SELECT role FROM users Where token = @token", new { token });
    }
}