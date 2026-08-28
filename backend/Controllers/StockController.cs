using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection.Metadata.Ecma335;

[ApiController]
[Route("api")]
public class StockController : ControllerBase
{
    private readonly IDbConnection _db;
    public StockController(IDbConnection db) => _db = db;

    private Task<(int id, string role)?> GetUser(string? token)
        => AuthHelper.GetUser(_db, token);

    [HttpGet("my-stock")]
    public async Task<IActionResult> MyStock()
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await GetUser(token);
        if (user is null) return Unauthorized("Giriş Gerekli !");
        if (user.Value.role != "Bayi") return StatusCode(403, "Sadece bayiler");

        var sql = @"
            SELECT p.id AS product_id, p.name, c.name AS category,
                   p.price AS referans_fiyat,
                   ds.price AS benim_fiyatim,
                   ds.stock,
                   ROUND(p.price * COALESCE(p.min_oran, 80) / 100, 2) AS alt_sinir,
                   ROUND(p.price * COALESCE(p.max_oran, 120) / 100, 2) AS ust_sinir,
                   ROUND(p.price * 1.05, 2) AS onerilen,
                   (SELECT MAX(sm.created_at) FROM stock_movements sm
                     WHERE sm.dealer_id = ds.dealer_id
                       AND sm.product_id = ds.product_id) AS son_hareket
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
        if (user is null) return Unauthorized("Giriş Gerekli");
        if (user.Value.role != "Bayi") return StatusCode(403, "Sadece bayiler");

        var dealerId = user.Value.id;

        if (!await UrunListedeMi(dealerId, req.product_id))
            return NotFound("Bu ürün sizin stok listenizde yok");

        // Atomik guncelleme: okuma ve yazma tek sorguda, yaris kosulu yok
        var yeniStok = await _db.ExecuteScalarAsync<int?>(
            @"UPDATE dealer_stock SET stock = stock + @change
              WHERE dealer_id = @dealerId AND product_id = @pid
                AND stock + @change >= 0
              RETURNING stock",
            new { change = req.change, dealerId, pid = req.product_id });

        if (yeniStok is null)
            return BadRequest($"Stok eksiye düşemez. Çıkış: {req.change}");

        await _db.ExecuteAsync(
            @"INSERT INTO stock_movements (dealer_id, product_id, quantity)
              VALUES (@dealerId, @pid, @qty)",
            new { dealerId, pid = req.product_id, qty = req.change });

        return Ok(new { product_id = req.product_id, newStock = yeniStok.Value, change = req.change });
    }

    [HttpGet("my-stock/movements")]
    public async Task<IActionResult> MyMovements()
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await GetUser(token);
        if (user is null) return Unauthorized("Giriş Gerekli!");
        if (user.Value.role != "Bayi") return StatusCode(403, "Sadece bayiler");

        var sql = @"
            SELECT p.name AS urun, sm.quantity, sm.created_at
            FROM stock_movements sm
            JOIN products p ON p.id = sm.product_id
            JOIN categories c ON c.id = p.category_id
            WHERE sm.dealer_id = @dealerId
            ORDER BY sm.created_at DESC, sm.id DESC";

        var rows = await _db.QueryAsync(sql, new { dealerId = user.Value.id });
        return Ok(rows);
    }

    [HttpGet("my-stock/history")]
    public async Task<IActionResult> History()
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await GetUser(token);
        if (user is null) return Unauthorized("Giriş Gerekli!");
        if (user.Value.role != "Bayi") return StatusCode(403, "Sadece bayiler");

        var sql = @"
            SELECT
                DATE(created_at) AS tarih,
                COALESCE(SUM(CASE WHEN quantity > 0 THEN quantity ELSE 0 END), 0) AS giris,
                COALESCE(SUM(CASE WHEN quantity < 0 THEN -quantity ELSE 0 END), 0) AS cikis
            FROM stock_movements
            WHERE dealer_id = @dealerId
            GROUP BY DATE(created_at)
            ORDER BY tarih";

        var rows = await _db.QueryAsync(sql, new { dealerId = user.Value.id });
        return Ok(rows);
    }

    public record PriceReq(int product_id, decimal price);

    [HttpPut("my-stock/price")]
    public async Task<IActionResult> UpdatePrice([FromBody] PriceReq req)
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await GetUser(token);
        if (user is null) return Unauthorized("Giriş Gerekli");
        if (user.Value.role != "Bayi") return StatusCode(403, "Sadece bayiler");

        var dealerId = user.Value.id;

        var alt = await _db.ExecuteScalarAsync<decimal?>(
            "SELECT ROUND(p.price * COALESCE(p.min_oran, 80) / 100, 2) FROM products p WHERE p.id = @pid",
            new { pid = req.product_id });

        var ust = await _db.ExecuteScalarAsync<decimal?>(
            "SELECT ROUND(p.price * COALESCE(p.max_oran, 120) / 100, 2) FROM products p WHERE p.id = @pid",
            new { pid = req.product_id });

        if (alt is null || ust is null)
            return NotFound("Ürün bulunamadı ya da fiyat sınırları tanımsız");

        if (req.price < alt.Value || req.price > ust.Value)
            return BadRequest($"Fiyat {alt.Value} - {ust.Value} ₺ aralığında olmalı. Girilen: {req.price} ₺");

        var mevcutFiyat = await _db.ExecuteScalarAsync<decimal?>(
            "SELECT price FROM dealer_stock WHERE dealer_id = @dealerId AND product_id = @pid",
            new { dealerId, pid = req.product_id });

        if (mevcutFiyat is null && !await UrunListedeMi(dealerId, req.product_id))
            return NotFound("Bu ürün sizin listenizde yok");
        //
        //Bekleyen eski talebi iptal et
        //
        await _db.ExecuteAsync(
            @"UPDATE requests SET status = 'cancelled', resolved_at = NOW()
              WHERE dealer_id = @dealerId AND product_id = @pid
                AND type = 'price' AND status = 'pending'",
            new { dealerId, pid = req.product_id });
        //
        //Yeni talep oluştur
        //        
        var talepId = await _db.ExecuteScalarAsync<int>(
            @"INSERT INTO requests (dealer_id, product_id, type, old_price, new_price, status) 
              VALUES (@dealerId, @pid, 'price', @old, @new, 'pending')
              RETURNING id",
            new { dealerId, pid = req.product_id, old = mevcutFiyat, @new = req.price });

        return Ok(new { talep_id = talepId, durum = "Onay bekliyor", eski_fiyat = mevcutFiyat, yeni_fiyat = req.price });
    }

    private async Task<bool> UrunListedeMi(int dealerId, int product_id)
    {
        var sayi = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dealer_stock WHERE dealer_id = @dealerId AND product_id = @pid",
            new { dealerId, pid = product_id });
        return sayi > 0;

    }
}

