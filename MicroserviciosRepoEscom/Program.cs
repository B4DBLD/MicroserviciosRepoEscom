using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Repositorios;
using MicroserviciosRepoEscom.Servicios;
using Microsoft.AspNetCore.Cors.Infrastructure;

// Inicializar SQLite
SQLitePCL.Batteries.Init();


var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
