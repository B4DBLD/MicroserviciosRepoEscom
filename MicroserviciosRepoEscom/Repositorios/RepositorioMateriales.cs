using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Controllers;
using MicroserviciosRepoEscom.Models;
using Microsoft.Data.Sqlite;
using System.Text;

namespace MicroserviciosRepoEscom.Repositorios
{
    public class RepositorioMateriales: InterfazRepositorioMateriales
    {
        private readonly DBConfig _dbConfig;
        private readonly IConfiguration _configuration;
        private readonly string _uploadsFolder;
        private readonly ILogger<MaterialesController> _logger;

        public RepositorioMateriales(DBConfig dbConfig, IConfiguration configuration, ILogger<MaterialesController> logger)
        {
            _dbConfig = dbConfig;
            _configuration = configuration;
            _uploadsFolder = configuration["FileStorage:UploadsFolder"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            _logger = logger;
        }

        public async Task<IEnumerable<Material>> GetAllMateriales()
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, nombre, url, fechaCreacion, fechaActualizacion 
                FROM Material";

            using var reader = await command.ExecuteReaderAsync();
            var materiales = new List<Material>();

            while(await reader.ReadAsync())
            {
                materiales.Add(new Material
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Url = reader.GetString(2),
                    FechaCreacion = reader.GetString(3),
                    FechaActualizacion = reader.GetString(4)
                });
            }

            return materiales;
        }

        public async Task<MaterialConRelacionesDTO?> GetMaterialById(int id)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            // Obtener el material
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, nombre, url, tipoArchivo, rutaAcceso, fechaCreacion, fechaActualizacion 
                FROM Material 
                WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();

            if(!await reader.ReadAsync())
            {
                return null;
            }

            var material = new MaterialConRelacionesDTO
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Url = reader.GetString(2),
                TipoArchivo = reader.GetString(3),
                rutaAcceso = reader.GetString(4),
                FechaCreacion = reader.GetString(5),
                FechaActualizacion = reader.GetString(6),
                Autores = new List<Autor>(),
                Tags = new List<Tag>()
            };

            // Obtener los autores relacionados
            using(var authorCommand = connection.CreateCommand())
            {
                authorCommand.CommandText = @"
                    SELECT a.id, a.nombre, a.apellido, a.email, a.fechaCreacion, a.fechaActualizacion 
                    FROM Autor a
                    JOIN AutorMaterial am ON a.id = am.autorId
                    WHERE am.materialId = @materialId";
                authorCommand.Parameters.AddWithValue("@materialId", id);

                using var authorReader = await authorCommand.ExecuteReaderAsync();

                while(await authorReader.ReadAsync())
                {
                    material.Autores.Add(new Autor
                    {
                        Id = authorReader.GetInt32(0),
                        Nombre = authorReader.GetString(1),
                        Apellido = authorReader.GetString(2),
                        Email = authorReader.GetString(3),
                        FechaCreacion = authorReader.GetString(4),
                        FechaActualizacion = authorReader.GetString(5)
                    });
                }
            }

            // Obtener los tags relacionados
            using(var tagCommand = connection.CreateCommand())
            {
                tagCommand.CommandText = @"
                    SELECT t.id, t.name 
                    FROM Tag t
                    JOIN MaterialTag mt ON t.id = mt.tagId
                    WHERE mt.materialId = @materialId";
                tagCommand.Parameters.AddWithValue("@materialId", id);

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

            return material;
        }

        public async Task<int> CreateMaterial(MaterialCreateDTO material, string? fileUrl = null, string? tipoArchivo = null)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Determinar la ruta de acceso según el tipo
                string rutaAcceso;
                if(tipoArchivo == "PDF")
                {
                    // Para PDF, guardar la ruta física completa
                    rutaAcceso = Path.Combine(_uploadsFolder, fileUrl);
                }
                else // ZIP
                {
                    // Para ZIP, guardar la URL del servicio Docker
                    string dockerUrl = _configuration["DockerViewerService:BaseUrl"] ?? "http://158.23.160.166:8080/";
                    // Asumimos que el ID se generará después de la inserción, así que dejamos un placeholder
                    rutaAcceso = $"{dockerUrl}view?id="; // Se completará después
                }

                // Crear el material
                int materialId;
                using(var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
                INSERT INTO Material (nombre, url, tipoArchivo, rutaAcceso, fechaCreacion, fechaActualizacion)
                VALUES (@nombre, @url, @tipoArchivo, @rutaAcceso, datetime('now', 'utc'), datetime('now', 'utc'));
                SELECT last_insert_rowid();";

                    command.Parameters.AddWithValue("@nombre", material.Nombre);
                    command.Parameters.AddWithValue("@url", fileUrl);
                    command.Parameters.AddWithValue("@tipoArchivo", tipoArchivo);
                    command.Parameters.AddWithValue("@rutaAcceso", rutaAcceso);

                    materialId = (int)(long)await command.ExecuteScalarAsync();
                }

                // Para ZIP, ahora que tenemos el ID, actualizar la rutaAcceso con el ID correcto
                if(tipoArchivo == "ZIP")
                {
                    string dockerUrl = _configuration["DockerViewerService:BaseUrl"] ?? "http://158.23.160.166:8080/";
                    string rutaActualizada = $"{dockerUrl}view?id={materialId}";

                    using(var updateCommand = connection.CreateCommand())
                    {
                        updateCommand.Transaction = transaction;
                        updateCommand.CommandText = "UPDATE Material SET rutaAcceso = @rutaAcceso WHERE id = @id";
                        updateCommand.Parameters.AddWithValue("@rutaAcceso", rutaActualizada);
                        updateCommand.Parameters.AddWithValue("@id", materialId);
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }

                // Relacionar con autores
                if(material.AutorIds != null && material.AutorIds.Count > 0)
                {
                    foreach(var autorId in material.AutorIds)
                    {
                        using var authorCommand = connection.CreateCommand();
                        authorCommand.Transaction = transaction;
                        authorCommand.CommandText = @"
                    INSERT INTO AutorMaterial (autorId, materialId)
                    VALUES (@autorId, @materialId)";
                        authorCommand.Parameters.AddWithValue("@autorId", autorId);
                        authorCommand.Parameters.AddWithValue("@materialId", materialId);
                        await authorCommand.ExecuteNonQueryAsync();
                    }
                }

                // Relacionar con tags
                if(material.TagIds != null && material.TagIds.Count > 0)
                {
                    foreach(var tagId in material.TagIds)
                    {
                        using var tagCommand = connection.CreateCommand();
                        tagCommand.Transaction = transaction;
                        tagCommand.CommandText = @"
                    INSERT INTO MaterialTag (tagId, materialId)
                    VALUES (@tagId, @materialId)";
                        tagCommand.Parameters.AddWithValue("@tagId", tagId);
                        tagCommand.Parameters.AddWithValue("@materialId", materialId);
                        await tagCommand.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
                return materialId;
            }
            catch(Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error al crear el material");
                throw;
            }
        }

        public async Task<bool> UpdateMaterial(int id, MaterialUpdateDTO material)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            // Iniciar transacción para garantizar la integridad
            using var transaction = connection.BeginTransaction();

            try
            {
                // Actualizar el material
                using(var command = connection.CreateCommand())
                {
                    var sqlBuilder = new StringBuilder("UPDATE Material SET fechaActualizacion = datetime('now', 'utc')");

                    if(!string.IsNullOrEmpty(material.Nombre))
                        sqlBuilder.Append(", nombre = @nombre");

                    if(!string.IsNullOrEmpty(material.Url))
                        sqlBuilder.Append(", url = @url");

                    sqlBuilder.Append(" WHERE id = @id");

                    command.Transaction = transaction;
                    command.CommandText = sqlBuilder.ToString();
                    command.Parameters.AddWithValue("@id", id);

                    if(!string.IsNullOrEmpty(material.Nombre))
                        command.Parameters.AddWithValue("@nombre", material.Nombre);

                    if(!string.IsNullOrEmpty(material.Url))
                        command.Parameters.AddWithValue("@url", material.Url);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if(rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }

                // Actualizar relaciones con autores si se proporcionaron
                if(material.AutorIds != null && material.AutorIds.Count > 0)
                {
                    // Eliminar relaciones existentes
                    using(var deleteCommand = connection.CreateCommand())
                    {
                        deleteCommand.Transaction = transaction;
                        deleteCommand.CommandText = "DELETE FROM AutorMaterial WHERE materialId = @materialId";
                        deleteCommand.Parameters.AddWithValue("@materialId", id);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }

                    // Agregar nuevas relaciones
                    foreach(var autorId in material.AutorIds)
                    {
                        using var insertCommand = connection.CreateCommand();
                        insertCommand.Transaction = transaction;
                        insertCommand.CommandText = @"
                            INSERT INTO AutorMaterial (autorId, materialId)
                            VALUES (@autorId, @materialId)";
                        insertCommand.Parameters.AddWithValue("@autorId", autorId);
                        insertCommand.Parameters.AddWithValue("@materialId", id);
                        await insertCommand.ExecuteNonQueryAsync();
                    }
                }

                // Actualizar relaciones con tags si se proporcionaron
                if(material.TagIds != null)
                {
                    // Eliminar relaciones existentes
                    using(var deleteCommand = connection.CreateCommand())
                    {
                        deleteCommand.Transaction = transaction;
                        deleteCommand.CommandText = "DELETE FROM MaterialTag WHERE materialId = @materialId";
                        deleteCommand.Parameters.AddWithValue("@materialId", id);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }

                    // Agregar nuevas relaciones
                    foreach(var tagId in material.TagIds)
                    {
                        using var insertCommand = connection.CreateCommand();
                        insertCommand.Transaction = transaction;
                        insertCommand.CommandText = @"
                            INSERT INTO MaterialTag (tagId, materialId)
                            VALUES (@tagId, @materialId)";
                        insertCommand.Parameters.AddWithValue("@tagId", tagId);
                        insertCommand.Parameters.AddWithValue("@materialId", id);
                        await insertCommand.ExecuteNonQueryAsync();
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

        public async Task<bool> DeleteMaterial(int id)
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
                    command.CommandText = "DELETE FROM AutorMaterial WHERE materialId = @id";
                    command.Parameters.AddWithValue("@id", id);
                    await command.ExecuteNonQueryAsync();
                }

                // Eliminar relaciones en MaterialTag
                using(var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM MaterialTag WHERE materialId = @id";
                    command.Parameters.AddWithValue("@id", id);
                    await command.ExecuteNonQueryAsync();
                }

                // Eliminar relaciones en UserFavorites
                //using(var command = connection.CreateCommand())
                //{
                //    command.Transaction = transaction;
                //    command.CommandText = "DELETE FROM UserFavorites WHERE materialId = @id";
                //    command.Parameters.AddWithValue("@id", id);
                //    await command.ExecuteNonQueryAsync();
                //}

                //// Eliminar relaciones en UserSearch
                //using(var command = connection.CreateCommand())
                //{
                //    command.Transaction = transaction;
                //    command.CommandText = "DELETE FROM UserSearch WHERE materialId = @id";
                //    command.Parameters.AddWithValue("@id", id);
                //    await command.ExecuteNonQueryAsync();
                //}

                // Eliminar el material
                using(var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM Material WHERE id = @id";
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
        public async Task<IEnumerable<MaterialConRelacionesDTO>> GetMaterialesByAutorId(int autorId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            var materiales = new List<MaterialConRelacionesDTO>();

            // Obtener los IDs de materiales relacionados con el autor
            using(var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT DISTINCT m.id
                    FROM Material m
                    JOIN AutorMaterial am ON m.id = am.materialId
                    WHERE am.autorId = @autorId";
                command.Parameters.AddWithValue("@autorId", autorId);

                using var reader = await command.ExecuteReaderAsync();

                while(await reader.ReadAsync())
                {
                    int materialId = reader.GetInt32(0);
                    var material = await GetMaterialById(materialId);
                    if(material != null)
                    {
                        materiales.Add(material);
                    }
                }
            }

            return materiales;
        }

        public async Task<IEnumerable<MaterialConRelacionesDTO>> GetMaterialesByTagId(int tagId)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            var materiales = new List<MaterialConRelacionesDTO>();

            // Obtener los IDs de materiales relacionados con el tag
            using(var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT DISTINCT m.id
                    FROM Material m
                    JOIN MaterialTag mt ON m.id = mt.materialId
                    WHERE mt.tagId = @tagId";
                command.Parameters.AddWithValue("@tagId", tagId);

                using var reader = await command.ExecuteReaderAsync();

                while(await reader.ReadAsync())
                {
                    int materialId = reader.GetInt32(0);
                    var material = await GetMaterialById(materialId);
                    if(material != null)
                    {
                        materiales.Add(material);
                    }
                }
            }

            return materiales;
        }

        public async Task<IEnumerable<MaterialConRelacionesDTO>> SearchMateriales(string? autorNombre, List<string>? tags)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            // Construir consulta SQL con condiciones dinámicas
            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("SELECT DISTINCT m.id FROM Material m");

            // Agregar joins necesarios según filtros
            if(!string.IsNullOrEmpty(autorNombre))
            {
                sqlBuilder.AppendLine("JOIN AutorMaterial am ON m.id = am.materialId");
                sqlBuilder.AppendLine("JOIN Autor a ON am.autorId = a.id");
            }

            if(tags != null && tags.Count > 0)
            {
                sqlBuilder.AppendLine("JOIN MaterialTag mt ON m.id = mt.materialId");
                sqlBuilder.AppendLine("JOIN Tag t ON mt.tagId = t.id");
            }

            // Agregar condiciones de filtro
            bool hasCondition = false;
            if(!string.IsNullOrEmpty(autorNombre))
            {
                sqlBuilder.AppendLine("WHERE (a.nombre LIKE @autorNombre OR a.apellido LIKE @autorNombre)");
                hasCondition = true;
            }

            if(tags != null && tags.Count > 0)
            {
                if(hasCondition)
                {
                    sqlBuilder.AppendLine("AND t.name IN (");
                }
                else
                {
                    sqlBuilder.AppendLine("WHERE t.name IN (");
                    hasCondition = true;
                }

                for(int i = 0; i < tags.Count; i++)
                {
                    if(i > 0) sqlBuilder.Append(", ");
                    sqlBuilder.Append($"@tag{i}");
                }
                sqlBuilder.AppendLine(")");
            }

            using var command = connection.CreateCommand();
            command.CommandText = sqlBuilder.ToString();

            // Agregar parámetros
            if(!string.IsNullOrEmpty(autorNombre))
            {
                command.Parameters.AddWithValue("@autorNombre", $"%{autorNombre}%");
            }

            if(tags != null)
            {
                for(int i = 0; i < tags.Count; i++)
                {
                    command.Parameters.AddWithValue($"@tag{i}", tags[i]);
                }
            }

            // Ejecutar consulta y obtener resultados
            var materialIds = new List<int>();
            using(var reader = await command.ExecuteReaderAsync())
            {
                while(await reader.ReadAsync())
                {
                    materialIds.Add(reader.GetInt32(0));
                }
            }

            // Obtener detalles completos de cada material
            var materiales = new List<MaterialConRelacionesDTO>();
            foreach(var materialId in materialIds)
            {
                var material = await GetMaterialById(materialId);
                if(material != null)
                {
                    materiales.Add(material);
                }
            }

            return materiales;
        }

        public async Task<IEnumerable<MaterialConRelacionesDTO>> SearchMaterialesAvanzado(BusquedaDTO busqueda)
        {
            return await SearchMateriales(busqueda.AutorNombre, busqueda.Tags);
        }
    }
}
