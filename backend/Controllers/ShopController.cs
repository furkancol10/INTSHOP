using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Runtime.InteropServices;

[ApiController]
[Route("api")]
public class ShopController : ControllerBase
{
    private readonly IDbConnection _db;
    public ShopController(IDbConnection db) => _db = db;

    private async Task<string?> GetRole(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        return await _db.QueryFirstOrDefaultAsync<string?>(
            "SELECT role FROM users WHERE token = @token", new { token });
    }

    [HttpGet("shop")]
    public async Task<IActionResult> Shop([FromQuery] int offset = 0, [FromQuery] int limit = 12)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await GetRole(token);
        if (role is null) return Unauthorized("Giriş gerekli");
        if (role != "Kullanici") return StatusCode(403, "Sadece müşteriler");

        var sql= @"
            SELECT p.id AS product_id, p.name, p.price, p.image_url,
                    u.id AS dealer_id, u.username AS dealer_name, ds.stock
            FROM dealer_stock ds
            JOIN products p ON p.id = ds.product_id
            JOIN users u ON u.id = ds.dealer_id
            WHERE ds.stock > 0
            ORDER BY p.name, u.username
            LIMIT @adet OFFSET @baslangic";
        var rows = await _db.QueryAsync(sql, new { adet = limit, baslangic = offset});
        return Ok(rows);
    }
 }