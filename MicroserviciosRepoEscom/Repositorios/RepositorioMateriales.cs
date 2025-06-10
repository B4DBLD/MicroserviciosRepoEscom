using MicroserviciosRepoEscom.Conexion;
using MicroserviciosRepoEscom.Controllers;
using MicroserviciosRepoEscom.Models;
using Microsoft.Data.Sqlite;
using System;
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

        public async Task<IEnumerable<Material>> GetAllMateriales(int? userRol = null)
        {
            string url;
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            // Construir consulta basada en el rol
            string whereClause = userRol == 3
                ? "" // Admin ve todos
                : "WHERE disponible = 1"; // Alumno solo ve disponibles

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT m.id, m.nombre, m.url, m.tipoArchivo, m.disponible, m.status, 
                       COALESCE(u.nombre || ' ' || u.apellidoP || COALESCE(' ' || u.apellidoM, ''), 'Sin asignar') as creadoPor,
                       m.creadoPor as creadorId,
                       m.fechaCreacion, m.fechaActualizacion 
                FROM Material m
                LEFT JOIN Usuario u ON m.creadoPor = u.id
                {whereClause}"; // Solo materiales habilitados

            using var reader = await command.ExecuteReaderAsync();
            var materiales = new List<Material>();

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
                materiales.Add(new Material
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Url = reader.GetString(2),
                    TipoArchivo = reader.GetString(3),
                    Disponible = reader.GetInt32(4),
                    Status = reader.GetInt32(5),
                    CreadoPor = reader.GetString(6), // Nombre completo del creador
                    CreadorId = reader.IsDBNull(7) ? null : reader.GetInt32(7), // ID del creador
                    FechaCreacion = reader.GetString(8),
                    FechaActualizacion = reader.GetString(9)
                });
            }

            return materiales;
        }

        public async Task<MaterialConRelacionesDTO?> GetMaterialById(int id, int? userRol = null)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            // Construir consulta basada en el rol
            string whereClause = (userRol == 3 || userRol == 2)
                ? "WHERE m.id = @id" // Admin ve todos
                : "WHERE m.id = @id AND m.disponible = 1"; // Alumno solo ve disponibles


            
            // Obtener el material
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT m.id, m.nombre, m.url, m.tipoArchivo, m.disponible, m.status,
                       COALESCE(u.nombre || ' ' || u.apellidoP || COALESCE(' ' || u.apellidoM, ''), 'Sin asignar') as creadoPor,
                       m.creadoPor as creadorId,
                       m.fechaCreacion, m.fechaActualizacion 
                FROM Material m
                LEFT JOIN Usuario u ON m.creadoPor = u.id
                {whereClause}"; // Solo materiales habilitados
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
                Disponible = reader.GetInt32(4),
                Status = reader.GetInt32(5),
                CreadoPor = reader.GetString(6), // Nombre del creador
                CreadorId = reader.IsDBNull(7) ? null : reader.GetInt32(7), // ID del creador
                FechaCreacion = reader.GetString(8),
                FechaActualizacion = reader.GetString(9),
                Autores = new List<Autor>(),
                Tags = new List<Tag>()
            };

            // Obtener los autores relacionados
            using(var authorCommand = connection.CreateCommand())
            {
                authorCommand.CommandText = @"
                    SELECT a.id, a.nombre, a.apellidoP, a.apellidoM, a.email, a.fechaCreacion, a.fechaActualizacion 
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
                        ApellidoP = authorReader.GetString(2),
                        ApellidoM = authorReader.IsDBNull(3) ? null : authorReader.GetString(3), // Manejo de apellidoM opcional
                        Email = authorReader.GetString(4),
                        FechaCreacion = authorReader.GetString(5),
                        FechaActualizacion = authorReader.GetString(6)
                    });
                }
            }

            // Obtener los tags relacionados
            using(var tagCommand = connection.CreateCommand())
            {
                tagCommand.CommandText = @"
                    SELECT t.id, t.nombre 
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

                if (tipoArchivo == "LINK")
                {
                    rutaAcceso = fileUrl;
                }
                else
                {
                    rutaAcceso = Path.Combine(_uploadsFolder, fileUrl);
                }
                
                int disponible = (tipoArchivo == "PDF" || tipoArchivo == "LINK") ? 1 : 0;
                int status = (tipoArchivo == "PDF" || tipoArchivo == "LINK") ? 1 : 0;

                // Crear el material
                int materialId;
                using(var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
                    INSERT INTO Material (nombre, url, tipoArchivo, disponible, status, creadoPor, fechaCreacion, fechaActualizacion)
                    VALUES (@nombre, @url, @tipoArchivo, @disponible, @status, @creadoPor, datetime('now', 'utc'), datetime('now', 'utc'));
                    SELECT last_insert_rowid();";

                    command.Parameters.AddWithValue("@nombre", material.Nombre);
                    command.Parameters.AddWithValue("@url", rutaAcceso);
                    command.Parameters.AddWithValue("@tipoArchivo", tipoArchivo);
                    command.Parameters.AddWithValue("@disponible", disponible);
                    command.Parameters.AddWithValue("@status", status);
                    command.Parameters.AddWithValue("@creadoPor", material.CreadoPor);

                    materialId = (int)(long)await command.ExecuteScalarAsync();
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
                _logger.LogError(ex.InnerException.ToString(), ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> UpdateMaterial(int id, MaterialUpdateDTO material, string? fileUrl = null, string? tipoArchivo = null)
        {
            string rutaAcceso;
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            if (tipoArchivo == "LINK")
            {
                rutaAcceso = fileUrl;
            }
            else
            {
                rutaAcceso = Path.Combine(_uploadsFolder, fileUrl);
            }

            using var transaction = connection.BeginTransaction();

           try
           {
               // Verificar que el material exista
               bool materialExists;
               using(var checkCommand = connection.CreateCommand())
               {
                   checkCommand.Transaction = transaction;
                   checkCommand.CommandText = "SELECT COUNT(1) FROM Material WHERE id = @id";
                   checkCommand.Parameters.AddWithValue("@id", id);
                   
                   long count = (long)await checkCommand.ExecuteScalarAsync();
                   materialExists = count > 0;
               }
               
               if(!materialExists)
               {
                   transaction.Rollback();
                   return false;
               }

                var fieldsToUpdate = new List<string> { "fechaActualizacion = datetime('now', 'utc')" };
                var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.Parameters.AddWithValue("@id", id);

                if(!string.IsNullOrEmpty(material.NombreMaterial))
                {
                    fieldsToUpdate.Add("nombre = @nombre");
                    updateCommand.Parameters.AddWithValue("@nombre", material.NombreMaterial);
                }

                if(!string.IsNullOrEmpty(rutaAcceso))
                {
                    fieldsToUpdate.Add("url = @url");
                    updateCommand.Parameters.AddWithValue("@url", rutaAcceso);
                }

                if(!string.IsNullOrEmpty(tipoArchivo))
                {
                    fieldsToUpdate.Add("tipoArchivo = @tipoArchivo");
                    updateCommand.Parameters.AddWithValue("@tipoArchivo", tipoArchivo);

                    // Si cambió a ZIP, deshabilitar hasta revisión
                    if(tipoArchivo == "ZIP")
                    {
                        fieldsToUpdate.Add("disponible = 0");
                        fieldsToUpdate.Add("status = 0");
                    }
                    // Si cambió a PDF, habilitar automáticamente
                    else if(tipoArchivo == "PDF" || tipoArchivo == "LINK")
                    {
                        fieldsToUpdate.Add("disponible = 1");
                        fieldsToUpdate.Add("status = 1");
                    }
                }

                if(fieldsToUpdate.Count > 1) // Más que solo fechaActualizacion
                {
                    updateCommand.CommandText = $"UPDATE Material SET {string.Join(", ", fieldsToUpdate)} WHERE id = @id";
                    await updateCommand.ExecuteNonQueryAsync();
                }

                // Actualizar relaciones con autores si se proporcionaron
                if(material.Autores != null)
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
                    foreach(var autorId in material.AutoresIds)
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
           catch(Exception ex)
           {
               _logger.LogError(ex, $"Error al actualizar el material con ID {id}");
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

                //Eliminar relaciones en UserFavorites
                using(var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "DELETE FROM UserFavorites WHERE materialId = @id";
                    command.Parameters.AddWithValue("@id", id);
                    await command.ExecuteNonQueryAsync();
                }

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
        public async Task<IEnumerable<MaterialConRelacionesDTO>> GetMaterialesByAutorId(int autorId, int? userRol = null)
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
                    var material = await GetMaterialById(materialId, userRol);
                    if(material != null)
                    {
                        materiales.Add(material);
                    }
                }
            }

            return materiales;
        }

        public async Task<IEnumerable<MaterialConRelacionesDTO>> GetMaterialesByTagId(int tagId, int? userRol = null)
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
                    var material = await GetMaterialById(materialId, userRol);
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
                string[] autorTerms = autorNombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if(autorTerms.Length >= 2)
                {
                    // Múltiples palabras: buscar combinaciones flexibles
                    var autorConditions = new List<string>();

                    for(int i = 0; i < autorTerms.Length; i++)
                    {
                        autorConditions.Add($"a.nombre LIKE @autorTerm{i}");
                        autorConditions.Add($"a.apellidoP LIKE @autorTerm{i}");
                        autorConditions.Add($"a.apellidoM LIKE @autorTerm{i}");
                    }

                    sqlBuilder.AppendLine($"WHERE ({string.Join(" OR ", autorConditions)})");
                }
                else
                {
                    // Una palabra: buscar en cualquier campo
                    sqlBuilder.AppendLine("WHERE (a.nombre LIKE @autorTermino OR a.apellidoP LIKE @autorTermino OR a.apellidoM LIKE @autorTermino)");
                }

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
                string[] autorTerms = autorNombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if(autorTerms.Length >= 2)
                {
                    for(int i = 0; i < autorTerms.Length; i++)
                    {
                        command.Parameters.AddWithValue($"@autorTerm{i}", $"%{autorTerms[i]}%");
                    }
                }
                else
                {
                    command.Parameters.AddWithValue("@autorTermino", $"%{autorNombre}%");
                }
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

        public async Task<IEnumerable<MaterialConRelacionesDTO>> SearchMaterialesAvanzado(BusquedaDTO busqueda, int? userRol = null)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();


            // Convertir tags a enteros al inicio
            List<int> tagIds = new List<int>();
            if (busqueda.Tags != null && busqueda.Tags.Count > 0)
            {
                foreach (var tag in busqueda.Tags)
                {
                    try
                    {
                        int tagId = Convert.ToInt32(tag);
                        tagIds.Add(tagId);
                    }
                    catch (FormatException)
                    {
                        _logger.LogWarning($"Se ignoró el tag {tag} por no ser un ID válido");
                    }
                    catch (OverflowException)
                    {
                        _logger.LogWarning($"Se ignoró el tag {tag} por ser un número demasiado grande");
                    }
                }
            }

            // Construir consulta SQL con condiciones dinámicas
            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("SELECT DISTINCT m.id FROM Material m");

            bool hasAutorFilter = !string.IsNullOrEmpty(busqueda.AutorNombre);
            bool hasTagFilter = tagIds.Count > 0;
            bool hasNombreFilter = !string.IsNullOrEmpty(busqueda.MaterialNombre);

            // Agregar joins necesarios según filtros
            if (hasAutorFilter)
            {
                sqlBuilder.AppendLine("JOIN AutorMaterial am ON m.id = am.materialId");
                sqlBuilder.AppendLine("JOIN Autor a ON am.autorId = a.id");
            }

            if (hasTagFilter)
            {
                sqlBuilder.AppendLine("JOIN MaterialTag mt ON m.id = mt.materialId");
            }

            // Construir UNA SOLA cláusula WHERE
            List<string> whereConditions = new List<string>();

            // 1. Condición por rol basado en userId enviado
            if (userRol != 3) // Si no es admin, solo ver disponibles
            {
                whereConditions.Add("m.disponible = 1");
            }
            // Si es admin (rol = 3), ve todos los materiales

            // 2. Condición por nombre de material
            if (hasNombreFilter)
            {
                whereConditions.Add("m.nombre LIKE @nombre");
            }

            // 3. Condición por autor
            if (hasAutorFilter)
            {
                string[] autorTerms = busqueda.AutorNombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (autorTerms.Length >= 2)
                {
                    // Múltiples palabras: buscar combinaciones flexibles
                    var autorConditions = new List<string>();

                    for (int i = 0; i < autorTerms.Length; i++)
                    {
                        autorConditions.Add($"a.nombre LIKE @autorTerm{i}");
                        autorConditions.Add($"a.apellidoP LIKE @autorTerm{i}");
                        autorConditions.Add($"a.apellidoM LIKE @autorTerm{i}");
                    }

                    whereConditions.Add($"({string.Join(" OR ", autorConditions)})");
                }
                else
                {
                    // Una palabra: buscar en cualquier campo
                    whereConditions.Add("(a.nombre LIKE @autorTermino OR a.apellidoP LIKE @autorTermino OR a.apellidoM LIKE @autorTermino)");
                }
            }

            // 4. Condición por tags
            if (hasTagFilter)
            {
                List<string> tagConditions = new List<string>();
                for (int i = 0; i < tagIds.Count; i++)
                {
                    tagConditions.Add($"mt.tagId = @tagId{i}");
                }
                whereConditions.Add($"({string.Join(" OR ", tagConditions)})");
            }

            // Agregar cláusula WHERE si hay condiciones
            if (whereConditions.Count > 0)
            {
                sqlBuilder.AppendLine($"WHERE {string.Join(" AND ", whereConditions)}");
            }

            // Log de la consulta para depuración
            _logger.LogInformation($"SQL Query: {sqlBuilder}");

            using var command = connection.CreateCommand();
            command.CommandText = sqlBuilder.ToString();

            // Agregar parámetros
            if (hasNombreFilter)
            {
                command.Parameters.AddWithValue("@nombre", $"%{busqueda.MaterialNombre}%");
            }

            if (hasAutorFilter)
            {
                string[] autorTerms = busqueda.AutorNombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (autorTerms.Length >= 2)
                {
                    for (int i = 0; i < autorTerms.Length; i++)
                    {
                        command.Parameters.AddWithValue($"@autorTerm{i}", $"%{autorTerms[i]}%");
                    }
                }
                else
                {
                    command.Parameters.AddWithValue("@autorTermino", $"%{busqueda.AutorNombre}%");
                }
            }

            if (hasTagFilter)
            {
                for (int i = 0; i < tagIds.Count; i++)
                {
                    command.Parameters.AddWithValue($"@tagId{i}", tagIds[i]);
                }
            }

            // Ejecutar consulta y obtener resultados
            var materialIds = new List<int>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    materialIds.Add(reader.GetInt32(0));
                }
            }

            // Log de resultados para depuración
            _logger.LogInformation($"Materiales encontrados: {materialIds.Count}");

            // Obtener detalles completos de cada material
            var materiales = new List<MaterialConRelacionesDTO>();
            foreach (var materialId in materialIds)
            {
                var material = await GetMaterialById(materialId, userRol);
                if (material != null)
                {
                    materiales.Add(material);
                }
            }

            return materiales;
        }

        public async Task<IEnumerable<Material>> GetMaterialPorCreador(int userId)
        {
            string url;
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT m.id, m.nombre, m.url, m.tipoArchivo, m.disponible, m.status, 
                       COALESCE(u.nombre, 'Sin asignar') as creadoPor,
                       m.fechaCreacion, m.fechaActualizacion 
                FROM Material m
                LEFT JOIN Usuario u ON m.creadoPor = u.id
                WHERE m.creadoPor = @userId
                ORDER BY m.fechaCreacion DESC";
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = await command.ExecuteReaderAsync();
            var materiales = new List<Material>();

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
                materiales.Add(new Material
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Url = url,
                    TipoArchivo = reader.GetString(3),
                    Disponible = reader.GetInt32(4),
                    Status = reader.GetInt32(5),
                    CreadoPor = reader.GetString(6), // Nombre del creador
                    FechaCreacion = reader.GetString(7),
                    FechaActualizacion = reader.GetString(8)
                });
            }

            return materiales;

        }

        public async Task<bool> CambiarDisponibilidad(int materialId, int disponible)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Material 
                SET disponible = @disponible, fechaActualizacion = datetime('now', 'utc')
                WHERE id = @id";

            command.Parameters.AddWithValue("@id", materialId);
            command.Parameters.AddWithValue("@disponible", disponible);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<int?> GetUserRol(int? id = null)
        {
            using var connection = new SqliteConnection(_dbConfig.ConnectionString);
            await connection.OpenAsync();
            int rol = 0;

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT rol 
                FROM Usuario 
                WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                rol = reader.GetInt32(0);
            }

            return rol;

        }


    }
}
