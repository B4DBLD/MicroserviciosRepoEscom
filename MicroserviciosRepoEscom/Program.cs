using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Models;
using MicroserviciosRepoEscom.Repositorios;
using MicroserviciosRepoEscom.Servicios;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Inicializar SQLite
SQLitePCL.Batteries.Init();


var builder = WebApplication.CreateBuilder(args);

// 1. Configurar l�mites globales para todos los servidores
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
    options.ValueLengthLimit = int.MaxValue; // L�mite de longitud para valores de formulario
    options.MultipartBodyLengthLimit = MaxFileSize; // L�mite de tama�o para cuerpos multipart
    options.MultipartHeadersLengthLimit = int.MaxValue; // L�mite para encabezados multipart
});

// 5. Configurar l�mites de streaming de formularios (si es aplicable)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = MaxFileSize;
});


// Registrar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuraci�n de la base de datos
var dbConfig = new DBConfig
{
    ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
};
builder.Services.AddSingleton(dbConfig);

// Registrar servicios de la aplicaci�n
builder.Services.AddScoped<InterfazRepositorioAutores, RepositorioAutores>();
builder.Services.AddScoped<InterfazRepositorioMateriales, RepositorioMateriales>();
builder.Services.AddScoped<InterfazRepositorioTags, RepositorioTags>();
builder.Services.AddScoped<InterfazRepositorioFavoritos, RepositorioFavoritos>();
builder.Services.AddScoped<InterfazRepositorioAdmin, RepositorioAdmin>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddHttpClient("ResendApi");
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<InterfazRepositorioHistorial, RepositorioHistorial>();

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.


app.Urls.Add("http://10.0.0.4:8081");
app.UseSwagger();
app.UseSwaggerUI();


app.Use(async (context, next) =>
{
    // Otra opci�n es establecer el l�mite din�micamente por solicitud
    context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = MaxFileSize;
    await next.Invoke();
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
