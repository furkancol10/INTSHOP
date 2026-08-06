using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

public record NewProduct(string name, int category_id, int stock, decimal price);

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IDbConnection _db;
    public ProductsController(IDbConnection db) => _db = db;
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sql = @"
            SELECT p.id, p.name, p.category_id, c.name AS category, p.stock, p.price, p.created_at
            FROM products p
            JOIN categories c ON c.id = p.category_id
            ORDER BY p.id";
        var rows = await _db.QueryAsync(sql);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewProduct p)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Bu işlem için yetkiniz yok !!!");

        var sql=@"
            INSERT INTO products (name, category_id, stock, price)
            VALUES (@name, @category_id, @stock, @price)
            RETURNING id";
        var id = await _db.ExecuteScalarAsync<int>(sql, p);
        return Ok(new { id });
        
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Bu işlem için yetkiniz yok !!!");
        
        var affected = await _db.ExecuteAsync(
            "DELETE FROM products WHERE id = @id", new { id });
        return affected > 0 ? Ok() : NotFound();
    }

    public record UpdateProduct(string name, int category_id, decimal price);

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProduct p)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if(role != "Admin") return StatusCode(403, "Bu işlem için yetkiniz yok!");

        var affected = await _db.ExecuteAsync(
            @"UPDATE products SET name = @name, category_id = @category_id, price = @price
              WHERE id = @id",
            new { id, p.name, p.category_id, p.price });

        return affected > 0 ? Ok() : NotFound();
    }
}