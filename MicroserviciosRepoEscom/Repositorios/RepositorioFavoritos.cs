using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Models;
using Microsoft.Data.Sqlite;

namespace MicroserviciosRepoEscom.Repositorios
{
    public class RepositorioFavoritos : InterfazRepositorioFavoritos
    {
        private readonly DBConfig _dbConfig;
        private readonly ILogger<RepositorioFavoritos> _logger;
        private readonly InterfazRepositorioMateriales _materialesRepository;

        public RepositorioFavoritos(DBConfig dbConfig, ILogger<RepositorioFavoritos> logger, InterfazRepositorioMateriales materialesRepository)
        {
            _dbConfig = dbConfig;
            _logger = logger;
            _materialesRepository = materialesRepository;
        }

        public async Task<bool> AddToFavorites(int userId, int materialId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Verificar si ya existe la relación
                using var checkCommand = connection.CreateCommand();
                checkCommand.Transaction = transaction;
                checkCommand.CommandText = @"
                    SELECT COUNT(1) FROM UserFavorites 
                    WHERE userId = @userId AND materialId = @materialId";
                checkCommand.Parameters.AddWithValue("@userId", userId);
                checkCommand.Parameters.AddWithValue("@materialId", materialId);

                long count = (long)await checkCommand.ExecuteScalarAsync();

                if(count > 0)
                {
                    // Ya existe en favoritos
                    transaction.Rollback();
                    return false;
                }

                // Agregar a favoritos
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
                    INSERT INTO UserFavorites (userId, materialId, fechaAgregado)
                    VALUES (@userId, @materialId, datetime('now', 'utc'))";
                insertCommand.Parameters.AddWithValue("@userId", userId);
                insertCommand.Parameters.AddWithValue("@materialId", materialId);

                await insertCommand.ExecuteNonQueryAsync();
                transaction.Commit();
                return true;
            }
            catch(Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, $"Error al agregar material {materialId} a favoritos del usuario {userId}");
                throw;
            }
        }

        public async Task<bool> RemoveFromFavorites(int userId, int materialId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM UserFavorites 
                WHERE userId = @userId AND materialId = @materialId";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@materialId", materialId);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> IsFavorite(int userId, int materialId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(1) FROM UserFavorites 
                WHERE userId = @userId AND materialId = @materialId";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@materialId", materialId);

            long count = (long)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<IEnumerable<MaterialFvoritoDTO>> GetUserFavorites(int userId)
        {
            string url =  string.Empty;
            string whereClause = string.Empty;
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            var materiales = new List<MaterialFvoritoDTO>();

            int? userRol = await _materialesRepository.GetUserRol(userId);

            if(userRol == 1)
            {
                whereClause = "AND m.disponible = 1";
            }
            else
            {
                whereClause = string.Empty;
            }



                // Obtener los materiales favoritos del usuario
                using var command = connection.CreateCommand();
            command.CommandText = @$"
                SELECT m.id, m.nombre, m.url, m.tipoArchivo, m.fechaCreacion, uf.fechaAgregado
                FROM Material m
                JOIN UserFavorites uf ON m.id = uf.materialId
                WHERE uf.userId = @userId {whereClause}
                ORDER BY uf.fechaAgregado DESC";
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = await command.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                if (reader.GetString(3) == "PDF")
                {
                    url = "";
                }
                else
                {
                    url = reader.GetString(2);
                }

                var material = new MaterialFvoritoDTO
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    url = url,
                    TipoArchivo = reader.GetString(3),
                    FechaCreacion = reader.GetString(4),
                    FechaAgregadoFavoritos = reader.GetString(5),
                    Autores = new List<Autor>(),
                    Tags = new List<Tag>()
                };

                materiales.Add(material);
            }

            // Obtener autores y tags para cada material
            foreach(var material in materiales)
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

                while(await autorReader.ReadAsync())
                {
                    material.Autores.Add(new Autor
                    {
                        Id = autorReader.GetInt32(0),
                        Nombre = autorReader.GetString(1),
                        ApellidoP = autorReader.GetString(2),
                        ApellidoM = autorReader.IsDBNull(3) ? null : autorReader.GetString(3),
                        Email = autorReader.GetString(4),
                        FechaCreacion = autorReader.GetString(5),
                        FechaActualizacion = autorReader.GetString(6)
                    });
                }

                // Obtener tags
                using var tagCommand = connection.CreateCommand();
                tagCommand.CommandText = @"
                    SELECT t.id, t.name
                    FROM Tag t
                    JOIN MaterialTag mt ON t.id = mt.tagId
                    WHERE mt.materialId = @materialId";
                tagCommand.Parameters.AddWithValue("@materialId", material.Id);

                using var tagReader = await tagCommand.ExecuteReaderAsync();

                while(await tagReader.ReadAsync())
                {
                    material.Tags.Add(new Tag
                    {
                        Id = tagReader.GetInt32(0),
                        Nombre = tagReader.GetString(1)
                    });
                }
            }

            return materiales;
        }

        public async Task<int> GetFavoritesCount(int materialId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM UserFavorites 
                WHERE materialId = @materialId";
            command.Parameters.AddWithValue("@materialId", materialId);

            long count = (long)await command.ExecuteScalarAsync();
            return (int)count;
        }

    }
}
