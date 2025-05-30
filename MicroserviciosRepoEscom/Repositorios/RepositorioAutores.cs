using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Controllers;
using MicroserviciosRepoEscom.Models;
using Microsoft.Data.Sqlite;
using System.Reflection.PortableExecutable;

namespace MicroserviciosRepoEscom.Repositorios
{
    public class RepositorioAutores : InterfazRepositorioAutores
    {
        private readonly DBConfig _dbConfig;
        private readonly ILogger<RepositorioAutores> _logger;

        public RepositorioAutores(DBConfig dbConfig, ILogger<RepositorioAutores> logger)
        {
            _dbConfig = dbConfig;
            _logger = logger;
        }

        public async Task<IEnumerable<Autor>> GetAllAutores()
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, nombre, apellidoP, apellidoM, email, fechaCreacion, fechaActualizacion 
                FROM Autor";

            using var reader = await command.ExecuteReaderAsync();
            var autores = new List<Autor>();

            while(await reader.ReadAsync())
            {
                autores.Add(new Autor
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    ApellidoP = reader.GetString(2),
                    ApellidoM = reader.IsDBNull(3) ? null : reader.GetString(3), // Manejo de apellidoM opcional
                    Email = reader.GetString(4),
                    FechaCreacion = reader.GetString(5),
                    FechaActualizacion = reader.GetString(6)
                });
            }

            return autores;
        }

        public async Task<Autor?> GetAutorById(int id)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, nombre, apellidoP, apellidoM, email, fechaCreacion, fechaActualizacion 
                FROM Autor 
                WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new Autor
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    ApellidoP = reader.GetString(2),
                    ApellidoM = reader.IsDBNull(3) ? null : reader.GetString(3), // Manejo de apellidoM opcional
                    Email = reader.GetString(4),
                    FechaCreacion = reader.GetString(5),
                    FechaActualizacion = reader.GetString(6)
                };
            }

            return null;
        }

        public async Task<Autor?> GetAutorByEmail(string email)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, nombre, apellidoP, apellidoM, email, fechaCreacion, fechaActualizacion 
                FROM Autor 
                WHERE email = @email";
            command.Parameters.AddWithValue("@email", email);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new Autor
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    ApellidoP = reader.GetString(2),
                    ApellidoM = reader.IsDBNull(3) ? null : reader.GetString(3), // Manejo de apellidoM opcional
                    Email = reader.GetString(4),
                    FechaCreacion = reader.GetString(5),
                    FechaActualizacion = reader.GetString(6)
                };
            }

            return null;
        }

        public async Task<Autor?> BuscarAutorPorNombreApellido(string nombre, string apellidoP, string apellidoM)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, nombre, apellidoP, apellidoM, email, fechaCreacion, fechaActualizacion 
                FROM Autor 
                WHERE nombre = @nombre 
                AND apellidoP = @apellidoP
                AND (apellidoM = @apellidoM OR (@apellidoM IS NULL AND apellidoM IS NULL))";
            command.Parameters.AddWithValue("@nombre", nombre);
            command.Parameters.AddWithValue("@apellidoP", apellidoP);
            command.Parameters.AddWithValue("@apellidoM", apellidoM ?? (object)DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new Autor
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    ApellidoP = reader.GetString(2),
                    ApellidoM = reader.IsDBNull(3) ? null : reader.GetString(3), // Manejo de apellidoM opcional
                    Email = reader.GetString(4),
                    FechaCreacion = reader.GetString(5),
                    FechaActualizacion = reader.GetString(6)
                };
            }

            return null;
        }

        public async Task<int> CreateAutor(Autor autor)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Autor (nombre, apellidoP, apellidoM, email, fechaCreacion, fechaActualizacion)
                VALUES (@nombre, @apellidoP, @apellidoM, @email, datetime('now', 'utc'), datetime('now', 'utc'));
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@nombre", autor.Nombre);
            command.Parameters.AddWithValue("@apellidoP", autor.ApellidoP);
            command.Parameters.AddWithValue("@apellidoM", (object)autor.ApellidoM ?? DBNull.Value); // Manejo de apellidoM opcional
            command.Parameters.AddWithValue("@email", autor.Email);

            long newId = (long)await command.ExecuteScalarAsync();
            return (int)newId;
        }

        public async Task<bool> UpdateAutor(int id, Autor autor)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Autor 
                SET nombre = @nombre, 
                    apellidoP = @apellidoP,
                    apellidoM = @apellidoM,
                    email = @email, 
                    fechaActualizacion = datetime('now', 'utc')
                WHERE id = @id";

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@nombre", autor.Nombre);
            command.Parameters.AddWithValue("@apellidoP", autor.ApellidoP);
            command.Parameters.AddWithValue("@apellidoM", autor.ApellidoM ?? (object)DBNull.Value); // Manejo de apellidoM opcional
            command.Parameters.AddWithValue("@email", autor.Email);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAutor(int id)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            // Iniciar transacción para garantizar la integridad
            using var transaction = connection.BeginTransaction();

            try
            {
                // Eliminar relaciones en AutorMaterial
                using(var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM AutorMaterial WHERE autorId = @id";
                    command.Parameters.AddWithValue("@id", id);
                    await command.ExecuteNonQueryAsync();
                }

                // Eliminar el autor
                using(var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM Autor WHERE id = @id";
                    command.Parameters.AddWithValue("@id", id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if(rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> CrearRelacion(int usuarioId, int autorId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO UsuarioAutor (usuarioId, autorId)
                    VALUES (@usuarioId, @autorId)
                    ON CONFLICT(usuarioId, autorId) DO NOTHING";

                command.Parameters.AddWithValue("@usuarioId", usuarioId);
                command.Parameters.AddWithValue("@autorId", autorId);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if(rowsAffected > 0)
                {
                    _logger.LogInformation($"Relación creada: Usuario {usuarioId} - Autor {autorId}");
                }
                else
                {
                    _logger.LogInformation($"Relación ya existía: Usuario {usuarioId} - Autor {autorId}");
                }

                return true; // Siempre retorna true porque ON CONFLICT DO NOTHING no falla
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al crear relación: Usuario {usuarioId} - Autor {autorId}");
                return false;
            }

        }

        public async Task<RelacionDTO> GetRelacion(int usuarioId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT autorId FROM UsuarioAutor WHERE usuarioId = @usuarioId";
            command.Parameters.AddWithValue("@usuarioId", usuarioId);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new RelacionDTO
                {
                    id = reader.GetInt32(0)
                };
            }

            return null;
        }

        public async Task<bool> EliminarRelacion(int? usuarioId, int? autorId)
        {
            string whereClause = string.Empty;
            if(usuarioId == null || autorId == null)
            {
                _logger.LogWarning("Se debe proporcional al menos un valor");
                return false;
            }

            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = @$"
                DELETE FROM UsuarioAutor 
                {whereClause}";

            if(usuarioId.HasValue && autorId.HasValue)
            {
                whereClause = "usuarioId = @usuarioId AND autorId = @autorId";
                command.Parameters.AddWithValue("@usuarioId", usuarioId.Value);
                command.Parameters.AddWithValue("@autorId", autorId.Value);
            }
            else if(usuarioId.HasValue)
            {
                whereClause = "usuarioId = @usuarioId";
                command.Parameters.AddWithValue("@usuarioId", usuarioId.Value);
            }
            else if(autorId.HasValue)
            {
                whereClause = "autorId = @autorId";
                command.Parameters.AddWithValue("@autorId", autorId.Value);
            }
            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;

        }
    }
}
