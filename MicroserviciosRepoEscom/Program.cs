using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Repositorios;
using MicroserviciosRepoEscom.Servicios;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Inicializar SQLite
SQLitePCL.Batteries.Init();


var builder = WebApplication.CreateBuilder(args);

// 1. Configurar límites globales para todos los servidores
const long MaxFileSize = 1073741824; // 1GB en bytes

// 2. Configurar Kestrel (servidor web integrado)
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = MaxFileSize;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// 3. Configurar IIS (si se utiliza como proxy inverso)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = MaxFileSize;
});

// 4. Configurar opciones de formularios para subida de archivos
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue; // Límite de longitud para valores de formulario
    options.MultipartBodyLengthLimit = MaxFileSize; // Límite de tamaño para cuerpos multipart
    options.MultipartHeadersLengthLimit = int.MaxValue; // Límite para encabezados multipart
});

// 5. Configurar límites de streaming de formularios (si es aplicable)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = MaxFileSize;
});


// Registrar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuración de la base de datos
var dbConfig = new DBConfig
{
    ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
};
builder.Services.AddSingleton(dbConfig);

// Registrar servicios de la aplicación
builder.Services.AddScoped<InterfazRepositorioAutores, RepositorioAutores>();
builder.Services.AddScoped<InterfazRepositorioMateriales, RepositorioMateriales>();
builder.Services.AddScoped<InterfazRepositorioTags, RepositorioTags>();
builder.Services.AddScoped<IFileService, FileService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    // Otra opción es establecer el límite dinámicamente por solicitud
    context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = MaxFileSize;
    await next.Invoke();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
