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
        var actor = await AuthHelper.GetUser(_db, token);
        if (actor is null || actor.Value.Role != "Admin") return StatusCode(403, "Sadece Admin");

        if (_db.State != ConnectionState.Open) _db.Open();
        using var tx = _db.BeginTransaction();
        try
        {
            var talep = await _db.QueryFirstOrDefaultAsync(
                "SELECT dealer_id, product_id, new_price, status FROM requests WHERE id = @id",
                new { id }, tx);

            if (talep is null) { tx.Rollback(); return NotFound("Talep bulunamadı"); }
            if ((string)talep.status != "pending") { tx.Rollback(); return BadRequest("Bu talep zaten karara bağlanmış"); }
            if (talep.new_price is null) { tx.Rollback(); return BadRequest("Talepte geçerli bir fiyat yok"); }

            int dealerId = (int)talep.dealer_id;
            int productId = (int)talep.product_id;
            decimal yeniFiyat = (decimal)talep.new_price;

            // Onay aninda fiyat sinirlarini yeniden dogrula - talep olusturulduktan sonra
            // urunun min/max_oran'i degismis olabilir (L-05).
            var sinirlar = await _db.QueryFirstOrDefaultAsync(
                @"SELECT ROUND(p.price * COALESCE(p.min_oran, 80) / 100, 2) AS alt,
                         ROUND(p.price * COALESCE(p.max_oran, 120) / 100, 2) AS ust
                  FROM products p WHERE p.id = @pid",
                new { pid = productId }, tx);

            if (sinirlar is null) { tx.Rollback(); return NotFound("Ürün bulunamadı"); }
            if (yeniFiyat < (decimal)sinirlar.alt || yeniFiyat > (decimal)sinirlar.ust)
            {
                tx.Rollback();
                return BadRequest($"Fiyat artık geçerli aralıkta değil ({sinirlar.alt} - {sinirlar.ust} ₺). Talebi reddedip bayiden yeni talep istemesini isteyin.");
            }

            //Fiyat uygula
            var etkilenen = await _db.ExecuteAsync(
                "UPDATE dealer_stock SET price = @price WHERE dealer_id = @dealerId AND product_id = @pid",
                new { price = yeniFiyat, dealerId, pid = productId }, tx);

            if (etkilenen == 0)
            {
                tx.Rollback();
                return BadRequest("Bu bayinin artık bu üründe stok kaydı yok, fiyat uygulanamadı");
            }

            //Talebi onayla
            await _db.ExecuteAsync(
                "UPDATE requests SET status = 'approved', admin_note = @note, resolved_at = NOW() WHERE id = @id",
                new { id, note = req?.note }, tx);

            await AuditLogger.Log(_db, actor.Value.Id, "request_approve", "request", id,
                new { dealerId, productId, yeniFiyat }, HttpContext.Connection.RemoteIpAddress?.ToString(), tx);

            tx.Commit();
            return Ok(new { id, durum = "onaylandi", uygulanan_fiyat = yeniFiyat });
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] KararReq? req)
    {
        var token = Request.Headers["Authorization"].ToString();
        var actor = await AuthHelper.GetUser(_db, token);
        if (actor is null || actor.Value.Role != "Admin") return StatusCode(403, "Sadece Admin");

        var durum = await _db.ExecuteScalarAsync<string?>(
            "SELECT status FROM requests WHERE id = @id", new { id });

        if(durum is null) return NotFound("Talep bulunamadı");
        if ( durum != "pending") return BadRequest("Bu talep zaten karara bağlanmış");

        await _db.ExecuteAsync(
            @"UPDATE requests SET status = 'rejected', admin_note = @note, resolved_at = NOW()
                WHERE id = @id",
                new { id ,note = req?.note });

        await AuditLogger.Log(_db, actor.Value.Id, "request_reject", "request", id,
            new { note = req?.note }, HttpContext.Connection.RemoteIpAddress?.ToString());

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