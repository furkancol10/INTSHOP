using Dapper;
using System.Data;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly IDbConnection _db;
    public RequestsController(IDbConnection db) => _db = db;

    public record KararReq(string? note);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string status = "pending")
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Sadece Admin");

        var sql = @"
            SELECT r.id, r.type, r.old_price, r.new_price, r.status, r.admin_note, r.created_at, r.resolved_at,
                u.username AS bayi, p.name AS urun,
                ROUND(p.price * COALESCE(p.min_oran, 80) / 100, 2) AS alt_sinir,
                ROUND(p.price * COALESCE(p.max_oran, 120) / 100, 2) AS ust_sinir,
                p.price AS referans_fiyat
            FROM requests r
            JOIN users u ON u.id = r.dealer_id
            JOIN products p ON p.id = r.product_id
            WHERE (@status = 'all' OR r.status = @status)
            ORDER BY r.created_at DESC";
        var rows = await _db.QueryAsync(sql, new { status });
        return Ok(rows);
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] KararReq? req)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Sadece Admin");

        var talep = await _db.QueryFirstOrDefaultAsync(
            "SELECT dealer_id, product_id, new_price, status FROM requests WHERE id = @id",
            new { id });

        if (talep is null) return NotFound("Talep bulunamadı");
        if ((string)talep.status != "pending") return BadRequest("Bu talep zaten karara bağlanmış");

        int dealerId = (int)talep.dealer_id;
        int productId = (int)talep.product_id;
        decimal yeni_Fiyat = (decimal)talep.new_price;

        //Fiyat uygula
        await _db.ExecuteAsync(
            "UPDATE dealer_stock SET price = @price WHERE dealer_id = @dealerId AND product_id = @pid",
            new {price = yeni_Fiyat, dealerId, pid = productId }); 
        //Talebi onayla
        await _db.ExecuteAsync(
            "UPDATE requests SET status = 'approved', admin_note = @note, resolved_at = NOW() WHERE id = @id",
            new { id ,note = req?.note });


        return Ok(new { id, durum = "onaylandi", uygulanan_fiyat = yeni_Fiyat });
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] KararReq? req)
    {
        var token = Request.Headers["Authorization"].ToString();
        var role = await AuthHelper.GetRole(_db, token);
        if (role != "Admin") return StatusCode(403, "Sadece Admin");

        var durum = await _db.ExecuteScalarAsync<string?>(
            "SELECT status FROM requests WHERE id = @id", new { id });

        if(durum is null) return NotFound("Talep bulunamadı");
        if ( durum != "pending") return BadRequest("Bu talep zaten karara bağlanmış");

        await _db.ExecuteAsync(
            @"UPDATE requests SET status = 'rejected', admin_note = @note, resolved_at = NOW()
                WHERE id = @id",
                new { id ,note = req?.note });

        return Ok(new { id , durum = "reddedildi"});
    }
    //
    // BAYİNİN KENDİ TALEPLERİ
    //
    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await AuthHelper.GetUser(_db, token);
        if (user is null) return Unauthorized();
        if (user.Value.Role != "Bayi") return StatusCode(403, "Sadece bayiler");

        var dealerId = user.Value.Id;

        var sql = @"
            SELECT r.id, r.old_price, r.new_price, r.status, r.admin_note,
                    r.created_at, r.resolved_at, p.name AS urun
            FROM requests r
            JOIN products p ON p.id = r.product_id
            WHERE r.dealer_id = @dealerId
            ORDER BY r.created_at DESC";
        var rows = await _db.QueryAsync(sql, new { dealerId });
        return Ok(rows);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var token = Request.Headers["Authorization"].ToString();
        var user = await AuthHelper.GetUser(_db, token);
        if (user is null) return Unauthorized();
        if (user.Value.Role != "Bayi") return StatusCode(403, "Sadece bayiler");

        var dealerId = user.Value.Id;

        var etkilenen = await _db.ExecuteAsync(
            @"UPDATE requests SET status = 'cancelled', resolved_at = NOW()
              WHERE id = @id AND dealer_id = @dealerId AND status = 'pending'",
            new { id, dealerId });

        if (etkilenen == 0)
            return BadRequest("Bu talep iptal edilemez (size ait değil ya da zaten karara bağlanmış)");

        return Ok(new { id, durum = "iptal edildi" }); 
    }
}