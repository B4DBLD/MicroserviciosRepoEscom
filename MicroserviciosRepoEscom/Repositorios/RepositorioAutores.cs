using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Models;
using Microsoft.Data.Sqlite;

namespace MicroserviciosRepoEscom.Repositorios
{
    public class RepositorioAutores : InterfazRepositorioAutores
    {
        private readonly DBConfig _dbConfig;

        public RepositorioAutores(DBConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        public async Task<IEnumerable<Autor>> GetAllAutores()
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, nombre, apellido, email, fechaCreacion, fechaActualizacion 
                FROM Autor";

            using var reader = await command.ExecuteReaderAsync();
            var autores = new List<Autor>();

            while(await reader.ReadAsync())
            {
                autores.Add(new Autor
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Apellido = reader.GetString(2),
                    Email = reader.GetString(3),
                    FechaCreacion = reader.GetString(4),
                    FechaActualizacion = reader.GetString(5)
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
                SELECT id, nombre, apellido, email, fechaCreacion, fechaActualizacion 
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
                    Apellido = reader.GetString(2),
                    Email = reader.GetString(3),
                    FechaCreacion = reader.GetString(4),
                    FechaActualizacion = reader.GetString(5)
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
                SELECT id, nombre, apellido, email, fechaCreacion, fechaActualizacion 
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
                    Apellido = reader.GetString(2),
                    Email = reader.GetString(3),
                    FechaCreacion = reader.GetString(4),
                    FechaActualizacion = reader.GetString(5)
                };
            }

            return null;
        }

        public async Task<Autor?> BuscarAutorPorNombreApellido(string nombre, string apellido)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
        SELECT id, nombre, apellido, email, fechaCreacion, fechaActualizacion 
        FROM Autor 
        WHERE nombre = @nombre AND apellido = @apellido";
            command.Parameters.AddWithValue("@nombre", nombre);
            command.Parameters.AddWithValue("@apellido", apellido);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new Autor
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Apellido = reader.GetString(2),
                    Email = reader.GetString(3),
                    FechaCreacion = reader.GetString(4),
                    FechaActualizacion = reader.GetString(5)
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
                INSERT INTO Autor (nombre, apellido, email, fechaCreacion, fechaActualizacion)
                VALUES (@nombre, @apellido, @email, datetime('now', 'utc'), datetime('now', 'utc'));
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@nombre", autor.Nombre);
            command.Parameters.AddWithValue("@apellido", autor.Apellido);
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
                    apellido = @apellido, 
                    email = @email, 
                    fechaActualizacion = datetime('now', 'utc')
                WHERE id = @id";

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@nombre", autor.Nombre);
            command.Parameters.AddWithValue("@apellido", autor.Apellido);
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
    }
}
