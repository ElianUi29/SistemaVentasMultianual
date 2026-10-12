using BackendVentas.Services;
using SistemaVentasMultianual.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Inyección de dependencia de la base de datos y servicios
builder.Services.AddScoped<Conexion>();
builder.Services.AddScoped<UsuarioService>(); // Servicio con la lógica SQL de Auth/Usuarios

// 2. Política CORS permisiva para GitHub Pages, Ngrok y móviles
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

// Soporte para endpoints
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// 4. Activar la política de CORS de inmediato
app.UseCors("PermitirFrontend");

// Se mantiene comentado para evitar problemas con el túnel HTTP de Ngrok
// app.UseHttpsRedirection();

app.UseAuthorization();

// 5. Mapear rutas de los controladores
app.MapControllers();

app.Run();