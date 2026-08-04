using Dapper;
using System.Data;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MovementsController : ControllerBase
{
    private readonly IDbConnection _db;
    public MovementsController(IDbConnection db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if(role != "Admin") return StatusCode(403, "Sadece Admin");

        var sql = @"
            SELECT sm.id, u.username AS bayi, p.name AS urun,
                   sm.quantity, sm.created_at
            FROM stock_movements sm
            JOIN users u ON u.id = sm.dealer_id
            JOIN products p ON p.id = sm.product_id
            ORDER BY sm.created_at DESC, sm.id DESC";
        var rows = await _db.QueryAsync(sql);
        return Ok(rows);
    }
}