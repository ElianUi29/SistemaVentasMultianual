using SistemaVentasMultianual.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Inyección de dependencia del servicio de Conexión
builder.Services.AddScoped<Conexion>();

// 2. Política CORS totalmente permisiva para GitHub Pages, Ngrok y móviles
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

// Soporte para Swagger
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// 4. Activar la política de CORS de inmediato
app.UseCors("PermitirFrontend");

// NOTA: Se comenta UseHttpsRedirection para evitar que rompa el túnel HTTP de Ngrok
// app.UseHttpsRedirection();

app.UseAuthorization();

// 5. Mapear rutas de los controladores
app.MapControllers();

app.Run();