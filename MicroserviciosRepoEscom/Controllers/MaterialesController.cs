using Microsoft.AspNetCore.Mvc;
using MicroserviciosRepoEscom.Models;
using MicroserviciosRepoEscom.Repositorios;
using MicroserviciosRepoEscom.Servicios;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MicroserviciosRepoEscom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialesController : ControllerBase
    {
        private readonly InterfazRepositorioMateriales _materialesRepository;
        private readonly InterfazRepositorioAutores _autoresRepository;
        private readonly InterfazRepositorioTags _tagsRepository;
        private readonly IFileService _fileService;
        private readonly ILogger<MaterialesController> _logger;

        public MaterialesController(
            InterfazRepositorioMateriales materialesRepository,
            InterfazRepositorioAutores autoresRepository,
            InterfazRepositorioTags tagsRepository,
            IFileService fileService,
            ILogger<MaterialesController> logger)
        {
            _materialesRepository = materialesRepository;
            _autoresRepository = autoresRepository;
            _tagsRepository = tagsRepository;
            _fileService = fileService;
            _logger = logger;
        }

        // GET: api/Materiales
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMateriales()
        {
            try
            {
                var materiales = await _materialesRepository.GetAllMateriales();
                return Ok(materiales);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los materiales");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // GET: api/Materiales/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MaterialConRelacionesDTO>> GetMaterial(int id)
        {
            try
            {
                var material = await _materialesRepository.GetMaterialById(id);

                if(material == null)
                {
                    return NotFound();
                }

                if(material.TipoArchivo == "PDF")
                {
                    // Para PDF, leer el archivo y devolver el blob
                    try
                    {
                        var fileData = await _fileService.GetFile(material.Url);

                        // Crear un objeto anónimo con todas las propiedades originales más el blob
                        material.Url = Convert.ToBase64String(fileData);

                        return Ok(material);
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
            catch(Exception ex)
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
        public async Task<ActionResult<IEnumerable<MaterialConRelacionesDTO>>> SearchMateriales([FromQuery] string? materialNombre, [FromQuery] string? autorNombre, [FromQuery] List<string>? tags)
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
        public async Task<ActionResult<MaterialConRelacionesDTO>> UploadMaterial([FromForm] string nombre, [FromForm] string autores, [FromForm] string? tags, IFormFile archivo)
        {
            try
            {
                // Validar los datos de entrada
                if(string.IsNullOrEmpty(nombre))
                {
                    return BadRequest("El nombre del material es requerido");
                }

                if(string.IsNullOrEmpty(autores))
                {
                    return BadRequest("Debe especificar al menos un autor");
                }

                if(archivo == null || archivo.Length == 0)
                {
                    return BadRequest("Debe proporcionar un archivo");
                }

                // Validar el tipo de archivo
                var fileExtension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if(fileExtension != ".pdf" && fileExtension != ".zip")
                {
                    return BadRequest("Tipo de archivo no permitido. Los formatos aceptados son: PDF y ZIP.");
                }

                // Determinar el tipo de archivo
                string tipoArchivo = fileExtension == ".pdf" ? "PDF" : "ZIP";

                // Guardar el archivo físicamente
                string fileUrl = await _fileService.SaveFile(archivo);

                // Procesar los nombres de autores
                string[] nombreAutores = autores.Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToArray();

                // Procesar los nombres de tags (si se proporcionaron)
                string[] nombreTags = string.IsNullOrEmpty(tags) ? Array.Empty<string>() :
                    tags.Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToArray();

                // Obtener o crear autores
                List<int> autorIds = new List<int>();
                foreach(var nombreAutor in nombreAutores)
                {
                    // Dividir en nombre y apellido (si es posible)
                    string[] partes = nombreAutor.Split(' ', 2);
                    string nombreParte = partes[0];
                    string apellidoParte = partes.Length > 1 ? partes[1] : "";

                    // Buscar si el autor ya existe
                    var autor = await _autoresRepository.BuscarAutorPorNombreApellido(nombreParte, apellidoParte);

                    if(autor == null)
                    {
                        // Crear nuevo autor
                        var nuevoAutor = new Autor
                        {
                            Nombre = nombreParte,
                            Apellido = apellidoParte,
                            Email = $"{nombreParte.ToLower()}.{apellidoParte.ToLower()}@example.com" // Email genérico ya que es requerido
                        };

                        int autorId = await _autoresRepository.CreateAutor(nuevoAutor);
                        autorIds.Add(autorId);
                    }
                    else
                    {
                        autorIds.Add(autor.Id);
                    }
                }

                // Obtener o crear tags
                List<int> tagIds = new List<int>();
                foreach(var nombreTag in nombreTags)
                {
                    // Buscar si el tag ya existe
                    var tag = await _tagsRepository.BuscarTagPorNombre(nombreTag);

                    if(tag == null)
                    {
                        // Crear nuevo tag
                        var nuevoTag = new Tag
                        {
                            Nombre = nombreTag
                        };

                        int tagId = await _tagsRepository.CreateTag(nuevoTag);
                        tagIds.Add(tagId);
                    }
                    else
                    {
                        tagIds.Add(tag.Id);
                    }
                }

                // Crear el material con los IDs obtenidos
                var createDTO = new MaterialCreateDTO
                {
                    Nombre = nombre,
                    AutorIds = autorIds,
                    TagIds = tagIds
                };

                var materialId = await _materialesRepository.CreateMaterial(createDTO, fileUrl, tipoArchivo);

                // Obtener el material creado
                var material = await _materialesRepository.GetMaterialById(materialId);

                return CreatedAtAction(nameof(GetMaterial), new { id = materialId }, material);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al subir material");
                return StatusCode(500, "Error interno del servidor");
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
                    return NotFound();
                }

                // Verificar que los autores existan (si se proporcionaron)
                if(materialDTO.AutorIds != null)
                {
                    foreach(var autorId in materialDTO.AutorIds)
                    {
                        var autor = await _autoresRepository.GetAutorById(autorId);
                        if(autor == null)
                        {
                            return BadRequest($"No existe un autor con ID {autorId}");
                        }
                    }
                }

                // Verificar que los tags existan (si se proporcionaron)
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

                var result = await _materialesRepository.UpdateMaterial(id, materialDTO);

                if(result)
                {
                    return NoContent();
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