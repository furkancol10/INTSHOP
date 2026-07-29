using Npgsql;
using System.Data;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddDefaultPolicy(p=> 
p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.UseCors();

app.MapControllers();

app.Run();
