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

    private Task<string?> GetRole(string? token)
        => AuthHelper.GetRole(_db, token);

    [HttpGet("shop")]
    public async Task<IActionResult> Shop([FromQuery] int offset = 0, [FromQuery] int limit = 20, int? kategori = null, string? arama = null)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await GetRole(token);
        if (role is null) return Unauthorized("Giriş gerekli");
        if (role != "Kullanici") return StatusCode(403, "Sadece müşteriler");

        // Her üründen mağaza ekranında tek satır: en ucuz bayi, fiyat eşitse stoğu en yüksek bayi
        var sql= @"
            SELECT * FROM (
                SELECT DISTINCT ON (p.id)
                    p.id AS product_id, p.name, p.image_url, ds.price, ds.stock,
                    u.id AS dealer_id, u.username AS dealer_name,
                    (SELECT COUNT(*) FROM dealer_stock ds2
                      WHERE ds2.product_id = p.id AND ds2.stock > 0 AND ds2.price IS NOT NULL) AS bayi_sayisi
                FROM dealer_stock ds
                JOIN products p ON p.id = ds.product_id
                JOIN users u ON u.id = ds.dealer_id
                WHERE ds.stock > 0 AND ds.price IS NOT NULL
                    AND (@kategori IS NULL OR p.category_id = @kategori)
                    AND (@arama IS NULL OR p.name ILIKE '%' || @arama || '%')
                ORDER BY p.id, ds.price ASC, ds.stock DESC
            ) en_iyi_teklif
            ORDER BY name
            LIMIT @adet OFFSET @baslangic";
        var rows = await _db.QueryAsync(sql, new { adet = limit, baslangic = offset, kategori, arama = string.IsNullOrWhiteSpace(arama) ? null : arama});
        return Ok(rows);
    }

    // ---------- ÜRÜN DETAYI: bir ürünü satan tüm bayiler ----------
    [HttpGet("shop/{id}")]
    public async Task<IActionResult> ShopProduct(int id)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await GetRole(token);
        if (role is null) return Unauthorized("Giriş gerekli");
        if (role != "Kullanici") return StatusCode(403, "Sadece müşteriler");

        var sql = @"
            SELECT p.id AS product_id, p.name, p.image_url, c.name AS category,
                   u.id AS dealer_id, u.username AS dealer_name, ds.price, ds.stock
            FROM dealer_stock ds
            JOIN products p ON p.id = ds.product_id
            JOIN users u ON u.id = ds.dealer_id
            LEFT JOIN categories c ON c.id = p.category_id
            WHERE ds.product_id = @id AND ds.stock > 0 AND ds.price IS NOT NULL
            ORDER BY ds.price ASC, ds.stock DESC";
        var teklifler = (await _db.QueryAsync(sql, new { id })).ToList();
        if (teklifler.Count == 0) return NotFound("Bu ürün şu an satışta değil");

        var ilk = teklifler[0];
        return Ok(new
        {
            product_id = (int)ilk.product_id,
            name = (string)ilk.name,
            image_url = (string?)ilk.image_url,
            category = (string?)ilk.category,
            teklifler
        });
    }
 }