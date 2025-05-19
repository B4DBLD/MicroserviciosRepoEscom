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

namespace MicroserviciosRepoEscom.Controllers
{
    [Route("repositorio/[controller]")]
    [ApiController]
    public class MaterialesController : ControllerBase
    {
        private readonly InterfazRepositorioMateriales _materialesRepository;
        private readonly InterfazRepositorioAutores _autoresRepository;
        private readonly InterfazRepositorioTags _tagsRepository;
        private readonly IFileService _fileService;
        private readonly IEmailService _emailService;
        private readonly string _uploadsFolder;
        private readonly ILogger<MaterialesController> _logger;

        public MaterialesController(
            InterfazRepositorioMateriales materialesRepository,
            InterfazRepositorioAutores autoresRepository,
            InterfazRepositorioTags tagsRepository,
            IFileService fileService,
            IEmailService emailService,
            ILogger<MaterialesController> logger)
        {
            _materialesRepository = materialesRepository;
            _autoresRepository = autoresRepository;
            _tagsRepository = tagsRepository;
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
                    return Ok(materiales);
                }
                else
                {
                    return BadRequest("El ID de usuario no puede ser nulo");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los materiales");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // GET: api/Materiales/5
        [HttpGet("{id}")]
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
                        return NotFound($"No se encontró el material");
                    }

                    if(!System.IO.File.Exists(material.Url))
                    {
                        return BadRequest("El archivo no fue encontrado");
                    }

                    if(material.TipoArchivo == "PDF")
                    {
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
                            return BadRequest("El archivo PDF no está disponible");
                        }
                    }
                    else
                    {
                        // Para ZIP u otros tipos, simplemente devolver el material tal cual
                        // Ya incluye rutaAcceso con la URL del servicio Docker
                        return Ok(material);
                    }
                }
                else
                {
                    return BadRequest("El ID de usuario no puede ser nulo");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el material con ID {id}");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id}/Detalles")]
        public async Task<ActionResult<MaterialConRelacionesDTO>> GetMaterial(int id, int? userId = null)
        {
            try
            {
                if(userId != null)
                {
                    int? userRol = await _materialesRepository.GetUserRol(userId);
                    var material = await _materialesRepository.GetMaterialById(id, userRol);

                    if(material == null)
                    {
                        return NotFound($"No se encontró el material");
                    }

                    else
                    {
                        // Para ZIP u otros tipos, simplemente devolver el material tal cual
                        // Ya incluye rutaAcceso con la URL del servicio Docker
                        return Ok(material);
                    }
                }else
                {
                    return BadRequest("El ID de usuario no puede ser nulo");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el material con ID {id}");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // GET: api/Materiales/ByAutor/5
        [HttpGet("ByAutor/{autorId}")]
        public async Task<ActionResult<IEnumerable<MaterialConRelacionesDTO>>> GetMaterialesByAutor(int autorId)
        {
            try
            {
                // Verificar que el autor exista
                var autor = await _autoresRepository.GetAutorById(autorId);
                if(autor == null)
                {
                    return NotFound($"No se encontró el autor con ID {autorId}");
                }

                var materiales = await _materialesRepository.GetMaterialesByAutorId(autorId);
                return Ok(materiales);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener materiales del autor con ID {autorId}");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // GET: api/Materiales/ByTag/5
        [HttpGet("ByTag/{tagId}")]
        public async Task<ActionResult<IEnumerable<MaterialConRelacionesDTO>>> GetMaterialesByTag(int tagId)
        {
            try
            {
                // Verificar que el tag exista
                var tag = await _tagsRepository.GetTagById(tagId);
                if(tag == null)
                {
                    return NotFound($"No se encontró el tag con ID {tagId}");
                }

                var materiales = await _materialesRepository.GetMaterialesByTagId(tagId);
                return Ok(materiales);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener materiales con el tag ID {tagId}");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // GET: api/Materiales/Search?autorNombre=nombre&tags=tag1&tags=tag2
        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<MaterialConRelacionesDTO>>> SearchMateriales([FromQuery] string? materialNombre, [FromQuery] string? autorNombre, [FromQuery] List<int>? tags)
        {

            try
            {
                var busqueda = new BusquedaDTO
                {
                    MaterialNombre = materialNombre,
                    AutorNombre = autorNombre,
                    Tags = tags
                };
                var materiales = await _materialesRepository.SearchMaterialesAvanzado(busqueda);
                return Ok(materiales);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al buscar materiales");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        

        // POST: api/Materiales/Upload
        [HttpPost("Upload")]
        public async Task<ActionResult<MaterialConRelacionesDTO>> UploadMaterial([FromForm] string datosJson, IFormFile archivo)
        {
            try
            {
                datosJson = Strings.Replace(datosJson, "'", "");

                // Validar que el archivo existe
                if(archivo == null || archivo.Length == 0)
                {
                    return BadRequest("Debe proporcionar un archivo");
                }

                // Validar extensión del archivo
                var fileExtension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if(fileExtension != ".pdf" && fileExtension != ".zip")
                {
                    return BadRequest("Tipo de archivo no permitido. Los formatos aceptados son: PDF y ZIP.");
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
                    return BadRequest($"Error al procesar el JSON: {ex.Message}");
                }

                // Validar los datos básicos
                if(string.IsNullOrEmpty(datos.NombreMaterial))
                {
                    return BadRequest("El nombre del material es requerido");
                }

                if(datos.Autores == null || datos.Autores.Count == 0)
                {
                    return BadRequest("Debe especificar al menos un autor");
                }

                // Determinar tipo de archivo
                string tipoArchivo = fileExtension == ".pdf" ? "PDF" : "ZIP";

                // Guardar el archivo usando el servicio existente
                string fileName = await _fileService.SaveFile(archivo);

                // Verificar que los TagIds son válidos
                if(datos.TagIds != null && datos.TagIds.Count > 0)
                {
                    foreach(var tagId in datos.TagIds)
                    {
                        var tag = await _tagsRepository.GetTagById(tagId);
                        if(tag == null)
                        {
                            return BadRequest($"El tag con ID {tagId} no existe");
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
                        return BadRequest($"El email '{autor.Email}' no es válido");
                    }

                    // Buscar por email
                    var existingAutor = await _autoresRepository.GetAutorByEmail(autor.Email);
                    if(existingAutor == null)
                    {
                        // Crear nuevo autor
                        var nuevoAutor = new Autor
                        {
                            Nombre = autor.Nombre,
                            Apellido = autor.Apellido,
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
                    TagIds = datos.TagIds
                };

                var materialId = await _materialesRepository.CreateMaterial(createDTO, fileName, tipoArchivo);
                var material = await _materialesRepository.GetMaterialById(materialId);

                if(tipoArchivo == "ZIP")
                {
                    try
                    {
                        string autorNombre = "Sin autor especificado";

                        if(datos.Autores != null && datos.Autores.Count > 0)
                        {
                            var nombresAutores = datos.Autores.Select(autor =>
                                $"{autor.Nombre} {autor.Apellido}".Trim()
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

                return CreatedAtAction(nameof(GetMaterial), new { id = materialId }, material);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al subir material");
                return StatusCode(500, "Error interno del servidor");
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
        public async Task<IActionResult> UpdateMaterial(int id, MaterialUpdateDTO materialDTO)
        {
            try
            {
                // Verificar que el material exista
                var existingMaterial = await _materialesRepository.GetMaterialById(id);
                if(existingMaterial == null)
                {
                    return NotFound($"No se encontró el material con ID {id}");
                }

                // Verificar que los autores existan si se proporcionaron
                if(materialDTO.Autores != null)
                {
                    foreach(var autorId in materialDTO.Autores)
                    {
                        var autor = await _autoresRepository.GetAutorById(autorId);
                        if(autor == null)
                        {
                            return BadRequest($"No existe un autor con ID {autorId}");
                        }
                    }
                }

                // Verificar que los tags existan si se proporcionaron
                if(materialDTO.TagIds != null)
                {
                    foreach(var tagId in materialDTO.TagIds)
                    {
                        var tag = await _tagsRepository.GetTagById(tagId);
                        if(tag == null)
                        {
                            return BadRequest($"No existe un tag con ID {tagId}");
                        }
                    }
                }

                // Ignorar valores por defecto o vacíos
                var updateDTO = new MaterialUpdateDTO
                {
                    // Solo pasar el nombre si no es el valor por defecto ("string") y no está vacío
                    Nombre = (materialDTO.Nombre != "string" && !string.IsNullOrEmpty(materialDTO.Nombre))
                        ? materialDTO.Nombre : null,

                    // Solo pasar la URL si no es el valor por defecto ("string") y no está vacía
                    Url = (materialDTO.Url != "string" && !string.IsNullOrEmpty(materialDTO.Url))
                        ? materialDTO.Url : null,

                    // Pasar autores y tags tal cual (null o la lista proporcionada)
                    Autores = materialDTO.Autores,
                    TagIds = materialDTO.TagIds
                };

                // Actualizar el material
                var result = await _materialesRepository.UpdateMaterial(id, updateDTO);
                if(result)
                {
                    // Obtener el material actualizado
                    var updatedMaterial = await _materialesRepository.GetMaterialById(id);
                    return Ok(updatedMaterial);
                }
                else
                {
                    return StatusCode(500, "Error al actualizar el material");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el material con ID {id}");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // DELETE: api/Materiales/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            try
            {
                // Verificar que el material exista
                var existingMaterial = await _materialesRepository.GetMaterialById(id);
                if(existingMaterial == null)
                {
                    return NotFound();
                }

                var result = await _materialesRepository.DeleteMaterial(id);

                if(result)
                {
                    return NoContent();
                }
                else
                {
                    return StatusCode(500, "Error al eliminar el material");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el material con ID {id}");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}