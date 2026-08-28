using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IDbConnection _db;
    public CategoriesController(IDbConnection db) => _db = db;

    public record NewCategory(string name, int? parent_id);

    // ---------- KATEGORİLERİ LİSTELE (hiyerarşik) ----------
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role is null) return Unauthorized("Giriş gerekli");

        var sql = @"
            SELECT c.id, c.name, c.parent_id,
                   ust.name AS parent_name,
                   (SELECT COUNT(*) FROM categories alt WHERE alt.parent_id = c.id) AS alt_sayisi
            FROM categories c
            LEFT JOIN categories ust ON ust.id = c.parent_id
            ORDER BY COALESCE(c.parent_id, c.id), c.id";
        var rows = await _db.QueryAsync(sql);
        return Ok(rows);
    }

    // ---------- KATEGORİ EKLE ----------
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewCategory c)
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await AuthHelper.GetUser(_db, token);
        if (user is null) return Unauthorized();
        if (user.Value.Role != "Admin") return StatusCode(403, "Bu işlem için yetkiniz yok");

        if (string.IsNullOrWhiteSpace(c.name))
            return BadRequest("Kategori adı zorunlu");

        var id = await _db.ExecuteScalarAsync<int>(
            "INSERT INTO categories (name, parent_id) VALUES (@name, @parent_id) RETURNING id",
            new { c.name, c.parent_id });

        await Denetim.Yaz(_db, HttpContext, user.Value.Id, user.Value.Role,
            "category.create", "categories", id,
            newValue: new { id, c.name, c.parent_id });

        return Ok(new { id, c.name, c.parent_id });
    }

    // ---------- KATEGORİ SİL ----------
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await AuthHelper.GetUser(_db, token);
        if (user is null) return Unauthorized();
        if (user.Value.Role != "Admin") return StatusCode(403, "Bu işlem için yetkiniz yok");

        // Alt kategorisi var mı?
        var altSayisi = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM categories WHERE parent_id = @id", new { id });
        if (altSayisi > 0)
            return BadRequest("Bu kategorinin alt kategorileri var, önce onları silin");

        // Ürünü var mı?
        var urunSayisi = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM products WHERE category_id = @id", new { id });
        if (urunSayisi > 0)
            return BadRequest($"Bu kategoride {urunSayisi} ürün var, önce onları taşıyın");

        var silinen = await _db.QueryFirstOrDefaultAsync(
            "SELECT name, parent_id FROM categories WHERE id = @id", new { id });
        if (silinen is null) return NotFound();

        var affected = await _db.ExecuteAsync("DELETE FROM categories WHERE id = @id", new { id });
        if (affected == 0) return NotFound();

        await Denetim.Yaz(_db, HttpContext, user.Value.Id, user.Value.Role,
            "category.delete", "categories", id,
            oldValue: new { silinen.name, silinen.parent_id });

        return Ok();
    }
}