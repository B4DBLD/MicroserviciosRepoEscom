using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Controllers;
using MicroserviciosRepoEscom.Models;
using Microsoft.Data.Sqlite;

namespace MicroserviciosRepoEscom.Repositorios
{
    public class RepositorioAdmin : InterfazRepositorioAdmin
    {

        private readonly DBConfig _dbConfig;
        private readonly IConfiguration _configuration;
        private readonly string _uploadsFolder;
        private readonly ILogger<AdminController> _logger;

        public RepositorioAdmin(DBConfig dbConfig, IConfiguration configuration, ILogger<AdminController> logger)
        {
            _dbConfig = dbConfig;
            _configuration = configuration;
            _uploadsFolder = configuration["FileStorage:UploadsFolder"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            _logger = logger;
        }

        public async Task<bool> CambiarDisponibilidad(int id, int disponible)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Material 
                SET disponible = @disponible, fechaActualizacion = datetime('now', 'utc')
                WHERE id = @id";

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@disponible", disponible);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<IEnumerable<Material>> GetAllMateriales()
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, nombre, url, tipoArchivo, disponible, fechaCreacion, fechaActualizacion 
                FROM Material"; // Solo materiales habilitados

            using var reader = await command.ExecuteReaderAsync();
            var materiales = new List<Material>();

            while(await reader.ReadAsync())
            {
                materiales.Add(new Material
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Url = reader.GetString(2),
                    TipoArchivo = reader.GetString(3),
                    Disponible = reader.GetInt32(4),
                    FechaCreacion = reader.GetString(5),
                    FechaActualizacion = reader.GetString(6)
                });
            }

            return materiales;
        }

    }
}
