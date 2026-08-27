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
        var user = await AuthHelper.GetUser(_db, token);
        return user?.Id;
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

        var urunVarMi = await _db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM dealer_stock WHERE product_id = @productId AND dealer_id = @dealerId)",
            new { productId = req.product_id, dealerId = req.dealer_id });
        if (!urunVarMi) return BadRequest("Bu ürün bu bayide satışta değil");

        // Stok kontrolu upsert'in kendi WHERE'ine gomulu - okuma ile yazma arasindaki
        // yaris koşulunu (TOCTOU) kapatir. affected == 0 ise stok yetersiz demektir.
        var affected = await _db.ExecuteAsync(
            @"INSERT INTO cart_items(user_id, product_id, dealer_id, quantity)
              SELECT @userId, @productId, @dealerId, 1
              WHERE (SELECT stock FROM dealer_stock WHERE product_id = @productId AND dealer_id = @dealerId) >= 1
              ON CONFLICT (user_id, product_id, dealer_id)
              DO UPDATE SET quantity = cart_items.quantity + 1
              WHERE (SELECT stock FROM dealer_stock WHERE product_id = @productId AND dealer_id = @dealerId)
                    >= cart_items.quantity + 1",
            new { userId = userId.Value, productId = req.product_id, dealerId = req.dealer_id });

        if (affected == 0) return BadRequest("Stok yetersiz");

        return Ok(new { eklendi = true });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> AdetGuncelle(int id, [FromBody] AdetReq req)
    {
        var userId = await KullaniciId();
        if (userId is null) return Unauthorized();
        if (req.quantity < 1) return BadRequest("Adet en az 1 olmalı");

        var satirVarMi = await _db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM cart_items WHERE id = @id AND user_id = @userId)",
            new { id, userId = userId.Value });
        if (!satirVarMi) return NotFound();

        // Stok kontrolu UPDATE'in kendi WHERE'ine gomulu - okuma ile yazma arasindaki
        // yaris koşulunu (TOCTOU) kapatir.
        var affected = await _db.ExecuteAsync(
            @"UPDATE cart_items c SET quantity = @quantity
              FROM dealer_stock ds
              WHERE c.id = @id AND c.user_id = @userId
                AND ds.product_id = c.product_id AND ds.dealer_id = c.dealer_id
                AND ds.stock >= @quantity",
            new { req.quantity, id, userId = userId.Value });

        if (affected == 0) return BadRequest("Stok yetersiz");

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