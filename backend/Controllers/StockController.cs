using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api")]
public class StockController : ControllerBase
{
    private readonly IDbConnection _db;
    public StockController(IDbConnection db) => _db = db;

    private async Task<(int id, string role)?> GetUser(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        var u = await _db.QueryFirstOrDefaultAsync(
            "SELECT id, role FROM users WHERE token = @token", new { token });
        if (u is null) return null;
        return ((int)u.id, (string)u.role);
    }

    [HttpGet("my-stock")]
    public async Task<IActionResult> MyStock()
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await GetUser(token);
        if (user is null) return Unauthorized("Giriş Gerekli !");
        if (user.Value.role != "Bayi") return StatusCode(403, "Sadece bayiler");

        var sql = @"
            SELECT p.id AS product_id, p.name, c.name AS category, p.price, ds.stock
            FROM dealer_stock ds
            JOIN products p ON p.id = ds.product_id
            JOIN categories c ON c.id = p.category_id
            WHERE ds.dealer_id = @dealerId
            ORDER BY p.name";
        var rows = await _db.QueryAsync(sql, new { dealerId = user.Value.id });
        return Ok(rows);
    }

    public record MovementReq(int product_id, int change);

    [HttpPost("my-stock/movement")]
    public async Task<IActionResult> AddMovement([FromBody] MovementReq req)
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await GetUser(token);
        if(user is null) return Unauthorized("Giriş Gerekli");
        if(user.Value.role != "Bayi") return StatusCode(403, "Sadece bayiler");

        var dealerId = user.Value.id;

        var current = await _db.QueryFirstOrDefaultAsync<int?>(
            "SELECT stock FROM dealer_stock WHERE dealer_id = @dealerId AND product_id =@pid",
            new  { dealerId, pid = req.product_id });

        if (current is null)
            return NotFound("Bu ürün sizin stok listenizde yok");

        var newStock = current.Value + req.change;
        if (newStock < 0)
            return BadRequest($"Stok eksiye düşemez. Mevcut: {current}, çıkış: {req.change}");

        await _db.ExecuteAsync(
            @"INSERT INTO stock_movements (dealer_id, product_id, quantity)
                VALUES (@dealerId, @pid, @qty)",
            new { dealerId, pid = req.product_id, qty = req.change });

        await _db.ExecuteAsync(
            @"UPDATE dealer_stock SET stock = @newStock
                WHERE dealer_id = @dealerId AND product_id = @pid",
            new { newStock, dealerId, pid = req.product_id });
        
        return Ok(new {product_id = req.product_id, newStock, change = req.change});
        
    }
}