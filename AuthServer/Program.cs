using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthServer.Data;

var builder = WebApplication.CreateBuilder(args);

// Configurar CORS para desenvolvimento local
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUnity", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurar banco de dados SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar autenticação JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
    throw new InvalidOperationException("JWT SecretKey não configurada em appsettings.json");

var keyBytes = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar migrações automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Habilitar Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Server API v1");
    c.RoutePrefix = string.Empty; // Swagger na raiz: http://localhost:5000
});

app.UseCors("AllowUnity");
app.UseAuthentication();
app.UseAuthorization();

// Rota de Health Check na raiz
app.MapGet("/health", () => Results.Ok(new
{
    status = "online",
    timestamp = DateTime.UtcNow,
    database = "connected",
    endpoints = new[]
    {
        "GET  /health",
        "GET  /swagger",
        "POST /api/auth/register",
        "POST /api/auth/login",
        "POST /api/auth/validate",
        "GET  /api/characters",
        "POST /api/characters"
    }
}));

app.MapControllers();

Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
Console.WriteLine("║         Auth Server Iniciado com Sucesso!            ║");
Console.WriteLine("╠═══════════════════════════════════════════════════════╣");
Console.WriteLine("║  Swagger UI: http://localhost:5000                    ║");
Console.WriteLine("║  Health:     http://localhost:5000/health             ║");
Console.WriteLine("║  API Base:   http://localhost:5000/api                ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════╝");

app.Run("http://localhost:5000");