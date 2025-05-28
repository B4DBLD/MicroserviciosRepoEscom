using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Controllers;
using MicroserviciosRepoEscom.Models;
using Microsoft.Data.Sqlite;

namespace MicroserviciosRepoEscom.Repositorios
{
    public class RepositorioAdmin : InterfazRepositorioAdmin
    {

        private readonly DBConfig _dbConfig;
        private readonly ILogger<AdminController> _logger;

        public RepositorioAdmin(DBConfig dbConfig, IConfiguration configuration, ILogger<AdminController> logger)
        {
            _dbConfig = dbConfig;
            _logger = logger;
        }

        public async Task<bool> CambiarStatus(int materialId, int status)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Material 
                SET status = @status, fechaActualizacion = datetime('now', 'utc')
                WHERE id = @id";

            command.Parameters.AddWithValue("@id", materialId);
            command.Parameters.AddWithValue("@status", status);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

    }
}
