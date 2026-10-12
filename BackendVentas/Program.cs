using SistemaVentasMultianual.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Inyección de dependencia del servicio de Conexión
// AddScoped crea una instancia por cada petición HTTP, ideal para servicios de datos
builder.Services.AddScoped<Conexion>();

// 2. Política CORS para permitir peticiones desde tu frontend (HTML/JS/Live Server)
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

// (Opcional recomendado) Soporte para Swagger en desarrollo
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// 4. Activar CORS (debe ir antes de MapControllers y UseAuthorization)
app.UseCors("PermitirFrontend");

// En desarrollo local puedes mantener o comentar UseHttpsRedirection si tu frontend corre en HTTP normal
app.UseHttpsRedirection();

app.UseAuthorization();

// 5. Mapear rutas de los controladores
app.MapControllers();

app.Run();