using System.Data;
using System.Data.Common;
using System.Net.Http.Headers;
using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;



namespace StokApi.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly IDbConnection _db;
    public CartController(IDbConnection db) => _db = db;

    private async Task<int?> KullaniciId()
    {
        var token = Request.Headers["Authorization"].ToString();
        return await _db.ExecuteScalarAsync<int?>(
            "SELECT id FROM users WHERE token = @token", new { token });
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = await KullaniciId();
        if (userId is null) return Unauthorized();

        var sql = @"
            SELECT c.id, c.product_id, c.dealer_id, c.quantity,
                    p.name AS urun, p.image_url,
                    d.username AS bayi,
                    ds.price, ds.stock,
                    (ds.price * c.quantity) AS satir_tutar
            FROM cart_items c
            JOIN products p ON p.id = c.product_id
            JOIN users d ON d.id = c.dealer_id
            JOIN dealer_stock ds ON ds.product_id = c.product_id AND ds.dealer_id = c.dealer_id
            WHERE c.user_id = @userId
            ORDER BY c.added_at DESC";

        var satirlar = (await _db.QueryAsync(sql, new { userId = userId.Value })).ToList();

        decimal toplam = satirlar.Sum(s => (decimal)s.satir_tutar);
        return Ok(new { satirlar, toplam, adet = satirlar.Count });
    }

    [HttpPost]
    public async Task<IActionResult> Ekle([FromBody] SepetEkleReq req)
    {
        var userId = await KullaniciId();
        if (userId is null) return Unauthorized();

        var stok = await _db.ExecuteScalarAsync<int?>(
            "SELECT stock FROM dealer_stock WHERE product_id = @productId AND dealer_id = @dealerId",
            new { productId = req.product_id, dealerId = req.dealer_id });
        if (stok is null) return BadRequest("Bu ürün bu bayide satışta değil");

        var mevcut = await _db.ExecuteScalarAsync<int?>(
            @"SELECT quantity FROM cart_items
              WHERE user_id = @userId AND product_id = @productId AND dealer_id = @dealerId",
        new { userId = userId.Value, productId = req.product_id, dealerId = req.dealer_id }) ?? 0;

        var yeniadet = mevcut + 1;
        if (yeniadet > stok.Value) return BadRequest($"Stok yetersiz (mevcut: {stok.Value})");

        await _db.ExecuteAsync(
            @"INSERT INTO cart_items(user_id, product_id, dealer_id, quantity)
              VALUES (@userId, @productId, @dealerId, 1)
              ON CONFLICT (user_id, product_id, dealer_id)
              DO UPDATE SET quantity = cart_items.quantity + 1",
            new { userId = userId.Value, productId = req.product_id, dealerId = req.dealer_id });

        return Ok(new { eklendi = true });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> AdetGuncelle(int id, [FromBody] AdetReq req)
    {
        var userId = await KullaniciId();
        if (userId is null) return Unauthorized();
        if (req.quantity < 1) return BadRequest("Adet en az 1 olmalı");

        var stok = await _db.ExecuteScalarAsync<int?>(
            @"SELECT ds.stock FROM cart_items c
              JOIN dealer_stock ds ON ds.product_id = c.product_id AND ds.dealer_id = c.dealer_id
              WHERE c.id = @id AND c.user_id = @userId",
              new { id, userId = userId.Value });
        if (stok is null) return NotFound();
        if (req.quantity > stok.Value) return BadRequest($"Stok yetersiz (mevcut: {stok.Value})");

        await _db.ExecuteAsync(
            "UPDATE cart_items SET quantity = @quantity WHERE id = @id AND user_id = @userId",
            new { req.quantity, id, userId = userId.Value });

        return Ok(new { guncellendi = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Sil(int id)
    {
        var userId = await KullaniciId();
        if (userId is null) return Unauthorized();

        var etkilenen = await _db.ExecuteAsync(
            "DELETE FROM cart_items WHERE id = @id AND user_id = @userId",
            new { id, userId = userId.Value });
        if (etkilenen == 0) return NotFound();

        return Ok(new { silindi = true });
    }

    [HttpDelete]
    public async Task<IActionResult> Bosalt()
    {
        var userId = await KullaniciId();
        if (userId is null) return Unauthorized();

        await _db.ExecuteAsync(
            "DELETE FROM cart_items WHERE user_id = @userId",
            new { userId = userId.Value });

        return Ok(new { bosaltildi = true });
    }
}

public record SepetEkleReq(int product_id, int dealer_id);
public record AdetReq(int quantity);