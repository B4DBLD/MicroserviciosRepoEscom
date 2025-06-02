using System;
using System.Data;
using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Models;
using Microsoft.Data.Sqlite;

namespace MicroserviciosRepoEscom.Repositorios
{
    public class RepositorioHistorial : InterfazRepositorioHistorial
    {
        private readonly DBConfig _dbConfig;
        private readonly ILogger<RepositorioHistorial> _logger;
        private readonly InterfazRepositorioMateriales _materialesRepository;

        public RepositorioHistorial(DBConfig dbConfig, ILogger<RepositorioHistorial> logger, InterfazRepositorioMateriales materialesRepository)
        {
            _dbConfig = dbConfig;
            _logger = logger;
            _materialesRepository = materialesRepository;
        }

        public async Task<bool> RegistrarConsulta(int userId, int materialId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            try
            {
                // Verificar si existe el registro
                using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = @"
            SELECT COUNT(*) FROM UserSearch 
            WHERE userId = @userId AND materialId = @materialId";
                checkCommand.Parameters.AddWithValue("@userId", userId);
                checkCommand.Parameters.AddWithValue("@materialId", materialId);

                long exists = (long)await checkCommand.ExecuteScalarAsync();

                if (exists > 0)
                {
                    // Hacer UPDATE explícito (esto SÍ dispara el trigger de UPDATE)
                    using var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = @"
                UPDATE UserSearch 
                SET ultimaConsulta = datetime('now', 'utc')
                WHERE userId = @userId AND materialId = @materialId";
                    updateCommand.Parameters.AddWithValue("@userId", userId);
                    updateCommand.Parameters.AddWithValue("@materialId", materialId);

                    await updateCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation($"Consulta ACTUALIZADA: Usuario {userId}, Material {materialId}");
                }
                else
                {
                    // Hacer INSERT (esto SÍ dispara el trigger de INSERT)
                    using var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                INSERT INTO UserSearch (userId, materialId, ultimaConsulta)
                VALUES (@userId, @materialId, datetime('now', 'utc'))";
                    insertCommand.Parameters.AddWithValue("@userId", userId);
                    insertCommand.Parameters.AddWithValue("@materialId", materialId);

                    await insertCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation($"Consulta INSERTADA: Usuario {userId}, Material {materialId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al registrar consulta: Usuario {userId}, Material {materialId}");
                return false;
            }
        }

        public async Task<IEnumerable<UserSearch>> GetHistorialUsuario(int userId)
        {
            string url;
            string whereClause;
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();
            int? userRol = null;

            // Primero limpiar materiales antiguos (más de 1 semana sin consulta)
            await LimpiarMaterialesAntiguos(userId);
            userRol = await _materialesRepository.GetUserRol(userId);

            if (userRol == 1)
            {
                whereClause = "AND m.disponible = 1";
            }
            else
            {
                whereClause = "";
            }

            var historial = new List<UserSearch>();

            using var command = connection.CreateCommand();
            command.CommandText = @$"
                SELECT 
                    us.materialId,
                    m.nombre,
                    m.url,
                    m.tipoArchivo,
                    us.ultimaConsulta
                FROM UserSearch us
                INNER JOIN Material m ON us.materialId = m.id
                WHERE us.userId = @userId  {whereClause}
                ORDER BY us.ultimaConsulta DESC";

            command.Parameters.AddWithValue("@userId", userId);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (reader.GetString(3) == "PDF")
                {
                    url = "";
                }
                else
                {
                    url = reader.GetString(2);
                }
                var material = new UserSearch
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Url = url,
                    TipoArchivo = reader.GetString(3),
                    FechaConsulta = reader.GetString(4),
                    Autores = new List<Autor>(),
                    Tags = new List<Tag>()
                };
                historial.Add(material);
            }

            // Obtener autores y tags para cada material
            foreach (var material in historial)
            {
                // Obtener autores
                using var autorCommand = connection.CreateCommand();
                autorCommand.CommandText = @"
                    SELECT a.id, a.nombre, a.apellidoP, a.apellidoM, a.email, a.fechaCreacion, a.fechaActualizacion
                    FROM Autor a
                    JOIN AutorMaterial am ON a.id = am.autorId
                    WHERE am.materialId = @materialId";
                autorCommand.Parameters.AddWithValue("@materialId", material.Id);

                using var autorReader = await autorCommand.ExecuteReaderAsync();

                while (await autorReader.ReadAsync())
                {
                    material.Autores.Add(new Autor
                    {
                        Id = autorReader.GetInt32(0),
                        Nombre = autorReader.GetString(1),
                        ApellidoP = autorReader.GetString(2),
                        ApellidoM = autorReader.IsDBNull(3) ? null : autorReader.GetString(3),
                        Email = autorReader.GetString(4)
                    });
                }

                // Obtener tags
                using var tagCommand = connection.CreateCommand();
                tagCommand.CommandText = @"
                    SELECT t.id, t.nombre
                    FROM Tag t
                    JOIN MaterialTag mt ON t.id = mt.tagId
                    WHERE mt.materialId = @materialId";
                tagCommand.Parameters.AddWithValue("@materialId", material.Id);

                using var tagReader = await tagCommand.ExecuteReaderAsync();

                while (await tagReader.ReadAsync())
                {
                    material.Tags.Add(new Tag
                    {
                        Id = tagReader.GetInt32(0),
                        Nombre = tagReader.GetString(1)
                    });
                }
            }

            return historial;
        }

        private async Task<bool> LimpiarMaterialesAntiguos(int userId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    DELETE FROM UserSearch 
                    WHERE userId = @userId 
                    AND datetime(ultimaConsulta) < datetime('now', '-7 days')";

                command.Parameters.AddWithValue("@userId", userId);

                int rowsDeleted = await command.ExecuteNonQueryAsync();

                if (rowsDeleted > 0)
                {
                    _logger.LogInformation($"Materiales antiguos eliminados del historial del usuario {userId}: {rowsDeleted} registros");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al limpiar materiales antiguos del usuario {userId}");
                return false;
            }
        }

        public async Task<bool> EliminarMaterialDelHistorial(int userId, int materialId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    DELETE FROM UserSearch 
                    WHERE userId = @userId AND materialId = @materialId";

                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@materialId", materialId);

                int rowsDeleted = await command.ExecuteNonQueryAsync();
                return rowsDeleted > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar material del historial: Usuario {userId}, Material {materialId}");
                return false;
            }
        }

        public async Task<bool> LimpiarTodoElHistorial(int userId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM UserSearch WHERE userId = @userId";
                command.Parameters.AddWithValue("@userId", userId);

                int rowsDeleted = await command.ExecuteNonQueryAsync();
                _logger.LogInformation($"Historial completo limpiado para usuario {userId}: {rowsDeleted} registros");

                return rowsDeleted > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al limpiar historial completo del usuario {userId}");
                return false;
            }
        }

    }
}
