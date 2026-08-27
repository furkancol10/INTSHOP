using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IDbConnection _db;
    public ProductsController(IDbConnection db) => _db = db;

    public record NewProduct(string name, int category_id, int stock, decimal price, string? image_url);
    public record UpdateProduct(string name, int category_id, decimal price, string? image_url);

    // ---------- ÜRÜNLERİ LİSTELE ----------
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sql = @"
            SELECT p.id, p.name, p.category_id,
                   c.name AS category,
                   ust.name AS parent_category,
                   p.stock, p.price, p.image_url,
                   p.min_oran, p.max_oran,
                   ROUND(p.price * COALESCE(p.min_oran, 80) / 100, 2) AS alt_sinir,
                   ROUND(p.price * COALESCE(p.max_oran, 120) / 100, 2) AS ust_sinir,
                   (SELECT COALESCE(SUM(ds.stock), 0) FROM dealer_stock ds
                     WHERE ds.product_id = p.id) AS toplam_stok
            FROM products p
            JOIN categories c ON c.id = p.category_id
            LEFT JOIN categories ust ON ust.id = c.parent_id
            ORDER BY p.id";
        var rows = await _db.QueryAsync(sql);
        return Ok(rows);
    }

    // ---------- ÜRÜN EKLE ----------
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewProduct p)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Bu işlem için yetkiniz yok");

        var sql = @"
            INSERT INTO products (name, category_id, stock, price, image_url)
            VALUES (@name, @category_id, @stock, @price, @image_url)
            RETURNING id";
        var id = await _db.ExecuteScalarAsync<int>(sql, p);

        // Yeni ürün için tüm bayilere 0 stoklu satır aç
        await _db.ExecuteAsync(
            @"INSERT INTO dealer_stock (dealer_id, product_id, stock)
              SELECT u.id, @productId, 0
              FROM users u
              WHERE u.role = 'Bayi'
                AND NOT EXISTS (
                  SELECT 1 FROM dealer_stock ds
                  WHERE ds.dealer_id = u.id AND ds.product_id = @productId
                )",
            new { productId = id });

        return Ok(new { id });
    }

    // ---------- ÜRÜN GÜNCELLE ----------
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProduct p)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Bu işlem için yetkiniz yok");

        var affected = await _db.ExecuteAsync(
            @"UPDATE products
              SET name = @name, category_id = @category_id, price = @price, image_url = @image_url
              WHERE id = @id",
            new { id, p.name, p.category_id, p.price, p.image_url });

        return affected > 0 ? Ok() : NotFound();
    }

    // ---------- ÜRÜN SİL ----------
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Bu işlem için yetkiniz yok");

        // Foreign key sırası: önce bağlı kayıtlar
        await _db.ExecuteAsync("DELETE FROM requests WHERE product_id = @id", new { id });
        await _db.ExecuteAsync("DELETE FROM stock_movements WHERE product_id = @id", new { id });
        await _db.ExecuteAsync("DELETE FROM cart_items WHERE product_id = @id", new { id });
        await _db.ExecuteAsync("DELETE FROM dealer_stock WHERE product_id = @id", new { id });

        var affected = await _db.ExecuteAsync("DELETE FROM products WHERE id = @id", new { id });
        return affected > 0 ? Ok() : NotFound();
    }
}