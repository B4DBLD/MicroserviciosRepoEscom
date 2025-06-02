using MicroserviciosRepoEscom.Models;
using MicroserviciosRepoEscom.Repositorios;
using MicroserviciosRepoEscom.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MicroserviciosRepoEscom.Controllers
{
    [Route("repositorio/[controller]")]
    [ApiController]
    public class MaterialesController : ControllerBase
    {
        private readonly InterfazRepositorioMateriales _materialesRepository;
        private readonly InterfazRepositorioAutores _autoresRepository;
        private readonly InterfazRepositorioTags _tagsRepository;
        private readonly InterfazRepositorioHistorial _historialRepository;
        public readonly InterfazRepositorioFavoritos _favoritosRepository;
        private readonly IFileService _fileService;
        private readonly IEmailService _emailService;
        private readonly string _uploadsFolder;
        private readonly ILogger<MaterialesController> _logger;

        public MaterialesController(
            InterfazRepositorioMateriales materialesRepository,
            InterfazRepositorioAutores autoresRepository,
            InterfazRepositorioTags tagsRepository,
            InterfazRepositorioHistorial historialRepository,
            InterfazRepositorioFavoritos favoritosRepository,
            IFileService fileService,
            IEmailService emailService,
            ILogger<MaterialesController> logger)
        {
            _materialesRepository = materialesRepository;
            _autoresRepository = autoresRepository;
            _tagsRepository = tagsRepository;
            _historialRepository = historialRepository;
            _favoritosRepository = favoritosRepository;
            _fileService = fileService;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: api/Materiales
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMateriales([FromQuery] int? userId = null)
        {
            try
            {
                if(userId != null)
                {
                    int? userRol = await _materialesRepository.GetUserRol(userId);
                        var materiales = await _materialesRepository.GetAllMateriales(userRol);
                    return Ok(ApiResponse<IEnumerable<Material>>.Success(materiales));
                }
                else
                {
                    return BadRequest(ApiResponse.Failure("El ID de usuario no puede ser nulo."));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los materiales");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // GET: api/Materiales/PorCreador
        [HttpGet("PorCreador/{userId}")]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterialesPorCreador(int userId)
        {
            try
            {
                if (userId != null)
                {
                    int? userRol = await _materialesRepository.GetUserRol(userId);
                    var materiales = await _materialesRepository.GetMaterialPorCreador(userId);
                    return Ok(ApiResponse<IEnumerable<Material>>.Success(materiales));
                }
                else
                {
                    return BadRequest(ApiResponse.Failure("El ID de usuario no puede ser nulo"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener los materiales creados por el usuario con ID {userId}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // GET: api/Materiales/Visualizar/5
        [HttpGet("Visualizar/{id}")]
        public async Task<ActionResult<MaterialConRelacionesDTO>> GetMaterialStream(int id, int? userId = null)
        {
            try
            {
                if(userId != null)
                {
                    int? userRol = await _materialesRepository.GetUserRol(userId);
                    var material = await _materialesRepository.GetMaterialById(id, userRol);

                    if(material == null)
                    {
                        return NotFound(ApiResponse.Failure($"No se encontró el material con ID {id}."));
                    }

                    if(material.TipoArchivo == "PDF" || material.TipoArchivo == "LINK")
                    {

                        if(!System.IO.File.Exists(material.Url))
                        {
                            return NotFound(ApiResponse.Failure("El archivo no fue encontrado"));
                        }
                        try
                        {
                            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{material.Nombre}.pdf\"");

                            // Habilita el cacheo para mejorar el rendimiento
                            Response.Headers.Add("Cache-Control", "public, max-age=3600");

                            // Permite la carga por partes
                            Response.Headers.Add("Accept-Ranges", "bytes");

                            // Streaming del archivo desde disco
                            var fileStream = new FileStream(
                                material.Url,
                                FileMode.Open,
                                FileAccess.Read,
                                FileShare.Read,
                                bufferSize: 4096,
                                useAsync: true
                            );

                            return new FileStreamResult(fileStream, "application/pdf")
                            {
                                EnableRangeProcessing = true // Habilita el soporte para solicitudes parciales
                            };
                        }
                        catch(FileNotFoundException)
                        {
                            return NotFound(ApiResponse.Failure("El archivo PDF no está disponible"));
                        }
                    }
                    else
                    {
                        // Para ZIP u otros tipos, simplemente devolver el material tal cual
                        // Ya incluye rutaAcceso con la URL del servicio Docker
                        return BadRequest(ApiResponse.Failure("No se pudo cargar el material debido a que no es un pdf"));
                    }
                }
                else
                {
                    return NotFound(ApiResponse.Failure("El ID de usuario no puede ser nulo"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el material con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        [HttpGet("{id}/Detalles")]
        public async Task<ActionResult<MaterialConRelacionesDTO>> GetMaterial(int id, int userId)
        {
            try
            {
                if (userId != null)
                {
                    int? userRol = await _materialesRepository.GetUserRol(userId);
                    var material = await _materialesRepository.GetMaterialById(id, userRol);
                    var favoritos = await _favoritosRepository.GetUserFavorites(userId);
                    if (favoritos != null && favoritos.Any(f => f.Id == id))
                    {
                        material.Favorito = true;
                    }
                    else
                    {
                        material.Favorito = false;
                    }


                    if (material == null)
                    {
                        return NotFound(ApiResponse.Failure($"No se encontró el material"));
                    }
                    else
                    {
                        // Para ZIP u otros tipos, simplemente devolver el material tal cual
                        // Ya incluye rutaAcceso con la URL del servicio Docker
                        if(material.Disponible == 1)
                        {
                            await _historialRepository.RegistrarConsulta(userId, material.Id);
                        }
                        return Ok(ApiResponse<MaterialConRelacionesDTO>.Success(material));
                    }

                    
                    

                    
                }else
                {
                    return BadRequest(ApiResponse.Failure("El ID de usuario no puede ser nulo"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el material con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor"));
            }
        }

        // GET: api/Materiales/ByAutor/5
        [HttpGet("ByAutor/{autorId}")]
        public async Task<ActionResult<IEnumerable<MaterialConRelacionesDTO>>> GetMaterialesByAutor(int autorId, int? userID = null)
        {
            try
            {
                // Verificar que el autor exista
                var autor = await _autoresRepository.GetAutorById(autorId);
                int? userRol = null;

                if (userID != null)
                {
                    userRol = await (_materialesRepository.GetUserRol(userID));
                }
                else
                {
                    userRol = await (_materialesRepository.GetUserRol(autorId));
                }

                if (autor == null)
                {
                    return NotFound(ApiResponse.Failure( $"No se encontró el autor con ID {autorId}"));
                }

                var materiales = await _materialesRepository.GetMaterialesByAutorId(autorId, userRol);
                return Ok(ApiResponse<IEnumerable<MaterialConRelacionesDTO>>.Success(materiales));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener materiales del autor con ID {autorId}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor"));
            }
        }

        // GET: api/Materiales/ByTag/5
        [HttpGet("ByTag/{tagId}")]
        public async Task<ActionResult<IEnumerable<MaterialConRelacionesDTO>>> GetMaterialesByTag(int tagId, int? userID = null)
        {
            try
            {
                // Verificar que el tag exista
                var tag = await _tagsRepository.GetTagById(tagId);
                int? userRol = await _materialesRepository.GetUserRol(userID);

                if (tag == null)
                {
                    return NotFound(ApiResponse.Failure($"No se encontró el tag con ID {tagId}"));
                }

                var materiales = await _materialesRepository.GetMaterialesByTagId(tagId, userRol);
                return Ok(ApiResponse<IEnumerable<MaterialConRelacionesDTO>>.Success(materiales));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener materiales con el tag ID {tagId}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // GET: api/Materiales/Search?autorNombre=nombre&tags=tag1&tags=tag2
        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<MaterialConRelacionesDTO>>> SearchMateriales([FromQuery] string? materialNombre, [FromQuery] string? autorNombre, [FromQuery] List<int>? tags, int? userId = null)
        {
            if (userId != null)
            {


                try
                {

                    var busqueda = new BusquedaDTO
                    {
                        MaterialNombre = materialNombre,
                        AutorNombre = autorNombre,
                        Tags = tags
                    };
                    int? userRol = await _materialesRepository.GetUserRol(userId);
                    var materiales = await _materialesRepository.SearchMaterialesAvanzado(busqueda, userRol);
                    return Ok(ApiResponse<IEnumerable<MaterialConRelacionesDTO>>.Success(materiales));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al buscar materiales");
                    return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
                }
            }
            else
            {
                return BadRequest(ApiResponse.Failure("El ID de usuario no puede ser nulo"));
            }
        }

        

        // POST: api/Materiales/Upload
        [HttpPost("Upload")]
        public async Task<ActionResult<MaterialConRelacionesDTO>> UploadMaterial(int userId, [FromForm] string? datosJson = null, IFormFile? archivo = null, [FromForm] string? Url = null)
        {
            string fileName = string.Empty;
            string tipoArchivo = string.Empty;
            var fileExtension = string.Empty;
            try
            {
                datosJson = Strings.Replace(datosJson, "'", "");

                if (datosJson == null || datosJson.Length == 0)
                {
                    return BadRequest(ApiResponse.Failure("El JSON de datos es requerido"));
                }

                // Verificar que se proporcione un archivo o un enlace, pero no ambos
                if(archivo == null && string.IsNullOrEmpty(Url))
                {
                    return BadRequest(ApiResponse.Failure("Debe proporcionar un archivo o un enlace"));
                }

                if(archivo != null && !string.IsNullOrEmpty(Url))
                {
                    return BadRequest(ApiResponse.Failure("No se puede proporcionar un archivo y un enlace al mismo tiempo"));
                }

                // Deserializar el JSON
                MaterialUploadDTO datos;
                try
                {
                    datos = JsonSerializer.Deserialize<MaterialUploadDTO>(datosJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch(Exception ex)
                {
                    return BadRequest(ApiResponse.Failure($"Error al procesar el JSON: {ex.Message}"));
                }

                // Validar los datos básicos
                if(string.IsNullOrEmpty(datos.NombreMaterial))
                {
                    return BadRequest(ApiResponse.Failure("El nombre del material es requerido"));
                }

                if(datos.Autores == null || datos.Autores.Count == 0)
                {
                    return BadRequest(ApiResponse.Failure("Debe especificar al menos un autor"));
                }

                // Validar que el archivo existe
                if(archivo != null)
                {
                    if(archivo.Length == 0) {
                        return BadRequest(ApiResponse.Failure("El archivo está vacío"));
                    }

                    // Validar extensión del archivo
                    fileExtension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                    if(fileExtension != ".pdf" && fileExtension != ".zip")
                    {
                        return BadRequest(ApiResponse.Failure("Tipo de archivo no permitido. Los formatos aceptados son: PDF y ZIP."));
                    }

                    // Determinar tipo de archivo
                    tipoArchivo = fileExtension == ".pdf" ? "PDF" : "ZIP";

                    // Guardar el archivo usando el servicio existente
                    fileName = await _fileService.SaveFile(archivo);

                }
                else
                {
                    // Es un enlace
                    // Validar que la URL sea potencialmente válida
                    if(!Uri.TryCreate(Url, UriKind.Absolute, out Uri uriResult) ||
                        (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                    {
                        return BadRequest(ApiResponse.Failure("La URL proporcionada no es válida"));
                    }

                    tipoArchivo = "LINK";
                    fileName =  Url; // Usar la URL como nombre del archivo
                }



                // Verificar que los TagIds son válidos
                if(datos.TagIds != null && datos.TagIds.Count > 0)
                {
                    foreach(var tagId in datos.TagIds)
                    {
                        var tag = await _tagsRepository.GetTagById(tagId);
                        if(tag == null)
                        {
                            return BadRequest(ApiResponse.Failure($"El tag con ID {tagId} no existe"));
                        }
                    }
                }

                // Procesar los autores (crear nuevos o usar existentes)
                List<int> autorIds = new List<int>();
                foreach(var autor in datos.Autores)
                {
                    // Validar que el email es válido
                    if(string.IsNullOrEmpty(autor.Email) || !IsValidEmail(autor.Email))
                    {
                        return BadRequest(ApiResponse.Failure($"El email '{autor.Email}' no es válido"));
                    }

                    // Buscar por email
                    var existingAutor = await _autoresRepository.GetAutorByEmail(autor.Email);
                    if(existingAutor == null)
                    {
                        // Crear nuevo autor
                        var nuevoAutor = new Autor
                        {
                            Nombre = autor.Nombre,
                            ApellidoP = autor.ApellidoP,
                            ApellidoM = autor.ApellidoM,
                            Email = autor.Email
                        };

                        int autorId = await _autoresRepository.CreateAutor(nuevoAutor);
                        autorIds.Add(autorId);
                    }
                    else
                    {
                        // Usar autor existente
                        autorIds.Add(existingAutor.Id);
                    }
                }

                // Crear el material
                var createDTO = new MaterialCreateDTO
                {
                    Nombre = datos.NombreMaterial,
                    AutorIds = autorIds,
                    TagIds = datos.TagIds,
                    CreadoPor = userId 
                };

                var materialId = await _materialesRepository.CreateMaterial(createDTO, fileName, tipoArchivo);
                var material = await _materialesRepository.GetMaterialById(materialId, 3);

                if(tipoArchivo == "ZIP")
                {
                    try
                    {
                        string autorNombre = "Sin autor especificado";

                        if(datos.Autores != null && datos.Autores.Count > 0)
                        {
                            var nombresAutores = datos.Autores.Select(autor =>
                                $"{autor.Nombre} {autor.ApellidoP}" +
                                (string.IsNullOrEmpty(autor.ApellidoM) ? "" : $" {autor.ApellidoM}")
                            ).ToList();

                            autorNombre = string.Join(", ", nombresAutores);
                        }
                        await _emailService.SendEmailAsync(datos.NombreMaterial, autorNombre);
                        _logger.LogInformation($"Notificación enviada para material ZIP ");
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, $"Error al enviar notificación ZIP");
                        
                        // No afecta la subida del archivo
                    }
                }

                return CreatedAtAction(nameof(GetMaterial), new { id = materialId }, ApiResponse<MaterialConRelacionesDTO>.Success(material));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al subir material");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        [HttpPut("disponibilidad")]
        public async Task<ActionResult> CambiarDisponibilidad(int materialId, [FromBody] DisponibilidadDTO disponibilidadDTO)
        {
            try
            {

                // Cambiar disponibilidad
                bool result = await _materialesRepository.CambiarDisponibilidad(materialId, disponibilidadDTO.Disponible);

                if (result)
                {
                    string estadoTexto = disponibilidadDTO.Disponible == 1 ? "Habilitado" : "Deshabilitado";

                    return Ok(ApiResponse<object>.Success(new
                    {
                        MaterialId = materialId,
                        StatusDTO = disponibilidadDTO.Disponible
                    },
                        $"Material {estadoTexto} exitosamente"));
                }
                else
                {
                    return StatusCode(500, ApiResponse.Failure("Error al cambiar el status del material"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cambiar el status del material {materialId}");
                return StatusCode(500, ApiResponse.Failure($"Error interno del servidor: {ex.Message}"));
            }
        }


        private bool IsValidEmail(string email)
        {
            if(string.IsNullOrEmpty(email))
                return false;

            try
            {
                // Validar formato de email
                var addr = new System.Net.Mail.MailAddress(email);
                if(addr.Address != email)
                    return false;

                // Obtener el dominio del email
                string dominio = email.Substring(email.IndexOf('@') + 1).ToLower();

                // Validar que el dominio sea uno de los permitidos
                string[] dominiosPermitidos = { "alumno.ipn.mx", "ipn.mx" };

                return dominiosPermitidos.Contains(dominio);
            }
            catch
            {
                return false;
            }
        }

        // PUT: api/Materiales/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaterial(int id, [FromForm] string? datosJson = null, IFormFile? nuevoArchivo = null, [FromForm] string? nuevaUrl = null)
        {
            try
            {
                string? nuevaRutaArchivo = null;
                string? nuevoTipoArchivo = null;
                MaterialUpdateDTO materialDTO = new MaterialUpdateDTO();
                // Verificar que el material exista
                var existingMaterial = await _materialesRepository.GetMaterialById(id, 3);
                if(existingMaterial == null)
                {
                    return NotFound(ApiResponse.Failure($"No se encontró el material con ID {id}"));
                }

                datosJson = Strings.Replace(datosJson, "'", "");

                if(datosJson == null || datosJson.Length == 0)
                {
                    return BadRequest(ApiResponse.Failure("El JSON de datos es requerido"));
                }

                if(!string.IsNullOrEmpty(datosJson) && datosJson != "string")
                {
                    try
                    {
                        materialDTO = JsonSerializer.Deserialize<MaterialUpdateDTO>(datosJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch(Exception ex)
                    {
                        return BadRequest(ApiResponse.Failure($"Error al procesar el JSON: {ex.Message}"));
                    }
                }

                if(nuevoArchivo != null && !string.IsNullOrEmpty(nuevaUrl) && nuevaUrl != "string")
                {
                    return BadRequest(ApiResponse.Failure("No se puede proporcionar un archivo y una URL al mismo tiempo"));
                }

                if(nuevoArchivo != null)
                {
                    if(nuevoArchivo.Length == 0)
                    {
                        return BadRequest(ApiResponse.Failure("El archivo está vacío"));
                    }

                    // Validar extensión del archivo
                    var fileExtension = Path.GetExtension(nuevoArchivo.FileName).ToLowerInvariant();
                    if(fileExtension != ".pdf" && fileExtension != ".zip")
                    {
                        return BadRequest(ApiResponse.Failure("Tipo de archivo no permitido. Los formatos aceptados son: PDF y ZIP."));
                    }

                    nuevoTipoArchivo = fileExtension == ".pdf" ? "PDF" : "ZIP";

                    try
                    {
                        // Eliminar archivo anterior si existe
                        if(!string.IsNullOrEmpty(existingMaterial.Url) && existingMaterial.TipoArchivo != "LINK")
                        {
                            await _fileService.DeleteFile(existingMaterial.Url);
                        }

                        // Guardar nuevo archivo
                        nuevaRutaArchivo = await _fileService.SaveFile(nuevoArchivo);
                    }
                    catch(Exception ex)
                    {
                        return StatusCode(500, ApiResponse.Failure($"Error al procesar el archivo: {ex.Message}"));
                    }
                }
                // Procesar nueva URL
                else if(!string.IsNullOrEmpty(nuevaUrl) && nuevaUrl != "string")
                {
                    if(!Uri.TryCreate(nuevaUrl, UriKind.Absolute, out Uri uriResult) ||
                        (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                    {
                        return BadRequest(ApiResponse.Failure("La URL proporcionada no es válida"));
                    }

                    // Eliminar archivo anterior si existe
                    if(!string.IsNullOrEmpty(existingMaterial.Url) && existingMaterial.TipoArchivo != "LINK")
                    {
                        await _fileService.DeleteFile(existingMaterial.Url);
                    }

                    nuevaRutaArchivo = nuevaUrl;
                    nuevoTipoArchivo = "LINK";
                }

                // Verificar que los tags existan si se proporcionaron
                if(materialDTO.TagIds != null)
                {
                    foreach(var tagId in materialDTO.TagIds)
                    {
                        var tag = await _tagsRepository.GetTagById(tagId);
                        if(tag == null)
                        {
                            return BadRequest(ApiResponse.Failure($"No existe un tag con ID {tagId}"));
                        }
                    }
                }
                foreach(var autor in materialDTO.Autores)
                {
                    // Validar que el email es válido
                    if(string.IsNullOrEmpty(autor.Email) || !IsValidEmail(autor.Email))
                    {
                        return BadRequest(ApiResponse.Failure($"El email '{autor.Email}' no es válido"));
                    }

                    // Buscar por email
                    var existingAutor = await _autoresRepository.GetAutorByEmail(autor.Email);
                    if(existingAutor == null)
                    {
                        // Crear nuevo autor
                        var nuevoAutor = new Autor
                        {
                            Nombre = autor.Nombre,
                            ApellidoP = autor.ApellidoP,
                            ApellidoM = autor.ApellidoM,
                            Email = autor.Email
                        };

                        int autorId = await _autoresRepository.CreateAutor(nuevoAutor);
                        materialDTO.AutoresIds.Add(autorId);
                    }
                    else
                    {
                        // Usar autor existente
                        materialDTO.AutoresIds.Add(existingAutor.Id);
                    }
                }

                // Ignorar valores por defecto o vacíos
                var updateDTO = new MaterialUpdateDTO
                {
                    // Solo pasar el nombre si no es el valor por defecto ("string") y no está vacío
                    NombreMaterial = (materialDTO.NombreMaterial != "string" && !string.IsNullOrEmpty(materialDTO.NombreMaterial))
                        ? materialDTO.NombreMaterial : null,

                    // Solo pasar la URL si no es el valor por defecto ("string") y no está vacía
                    Url = (materialDTO.Url != "string" && !string.IsNullOrEmpty(materialDTO.Url))
                        ? materialDTO.Url : null,

                    // Pasar autores y tags tal cual (null o la lista proporcionada)
                    Autores = materialDTO.Autores,
                    AutoresIds = materialDTO.AutoresIds,
                    TagIds = materialDTO.TagIds
                };

                // Actualizar el material
                var result = await _materialesRepository.UpdateMaterial(id, updateDTO, nuevaRutaArchivo,nuevoTipoArchivo);
                if(result)
                {
                    // Obtener el material actualizado
                    var updatedMaterial = await _materialesRepository.GetMaterialById(id, 3);
                    if (nuevoTipoArchivo == "ZIP")
                    {
                        try
                        {
                            string autorNombre = "Sin autor especificado";

                            if (materialDTO.Autores != null && materialDTO.Autores.Count > 0)
                            {
                                var nombresAutores = materialDTO.Autores.Select(autor =>
                                    $"{autor.Nombre} {autor.ApellidoP}" +
                                    (string.IsNullOrEmpty(autor.ApellidoM) ? "" : $" {autor.ApellidoM}")
                                ).ToList();

                                autorNombre = string.Join(", ", nombresAutores);
                            }
                            await _emailService.SendEmailAsync(materialDTO.NombreMaterial, autorNombre);
                            _logger.LogInformation($"Notificación enviada para material ZIP ");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error al enviar notificación ZIP");

                            // No afecta la subida del archivo
                        }
                    }
                    return Ok(ApiResponse<MaterialConRelacionesDTO>.Success (updatedMaterial, "El material se actualizo correctamente"));
                }
                else
                {
                    return StatusCode(500, ApiResponse.Failure("Error al actualizar el material"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el material con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // DELETE: api/Materiales/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            try
            {

                // Verificar que el material exista
                var existingMaterial = await _materialesRepository.GetMaterialById(id, 3);
                if(existingMaterial == null)
                {
                    return NotFound();
                }

                var result = await _materialesRepository.DeleteMaterial(id);

                if(result)
                {
                    return Ok(ApiResponse.Success("El material se ha eliminado correctamente."));
                }
                else
                {
                    return StatusCode(500, ApiResponse.Failure("Error al eliminar el material"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el material con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }
    }
}