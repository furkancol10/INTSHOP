using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IDbConnection _db;
    public CategoriesController(IDbConnection db) => _db = db;

    [HttpGet]
    public Task<IEnumerable<dynamic>> GetAll() =>
        _db.QueryAsync("SELECT id, name FROM categories ORDER BY name");
}