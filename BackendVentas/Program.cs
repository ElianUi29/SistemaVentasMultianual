using BackendVentas.Services;
using SistemaVentasMultianual.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Inyección de dependencia de la base de datos y servicios
builder.Services.AddScoped<UsuarioService>(); 
builder.Services.AddScoped<CajaService>();    
builder.Services.AddScoped<ClienteService>(); 
builder.Services.AddScoped<CompraService>(); // <-- Agregar esta línea
// 2. Política CORS permisiva
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. Registrar controladores de la API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// 4. Activar la política de CORS de inmediato
app.UseCors("PermitirFrontend");

app.UseAuthorization();

// 5. Mapear rutas de los controladores
app.MapControllers();

app.Run();