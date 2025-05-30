using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Models;
using Microsoft.Data.Sqlite;

namespace MicroserviciosRepoEscom.Repositorios
{
    public class RepositorioTags : InterfazRepositorioTags
    {
        private readonly DBConfig _dbConfig;

        public RepositorioTags(DBConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        public async Task<IEnumerable<Tag>> GetAllTags()
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, nombre FROM Tag";

            using var reader = await command.ExecuteReaderAsync();
            var tags = new List<Tag>();

            while(await reader.ReadAsync())
            {
                tags.Add(new Tag
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1)
                });
            }

            return tags;
        }

        public async Task<Tag?> GetTagById(int id)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, nombre FROM Tag WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new Tag
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1)
                };
            }

            return null;
        }

        public async Task<int> CreateTag(Tag tag)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Tag (nombre)
                VALUES (@nombre);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@nombre", tag.Nombre);

            long newId = (long)await command.ExecuteScalarAsync();
            return (int)newId;
        }

        public async Task<bool> UpdateTag(int id, Tag tag)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Tag SET nombre = @nombre WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@nombre", tag.Nombre);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<Tag?> BuscarTagPorNombre(string nombre)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, nombre FROM Tag WHERE nombre = @nombre";
            command.Parameters.AddWithValue("@nombre", nombre);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new Tag
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1)
                };
            }

            return null;
        }

        public async Task<bool> DeleteTag(int id)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            // Iniciar transacción para garantizar la integridad
            using var transaction = connection.BeginTransaction();

            try
            {
                // Eliminar relaciones en MaterialTag
                using(var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM MaterialTag WHERE tagId = @id";
                    command.Parameters.AddWithValue("@id", id);
                    await command.ExecuteNonQueryAsync();
                }

                // Eliminar el tag
                using(var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM Tag WHERE id = @id";
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
    }
}
