using System.Timers;
using MicroserviciosRepoEscom.Models;
using MicroserviciosRepoEscom.Repositorios;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviciosRepoEscom.Controllers
{
    [Route("repositorio/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly InterfazRepositorioTags _tagsRepository;
        private readonly ILogger<TagsController> _logger;

        public TagsController(InterfazRepositorioTags tagsRepository, ILogger<TagsController> logger)
        {
            _tagsRepository = tagsRepository;
            _logger = logger;
        }

        // GET: api/Tags
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tag>>> GetTags()
        {
            try
            {
                var tags = await _tagsRepository.GetAllTags();
                return Ok(ApiResponse<IEnumerable<Tag>>.Success( tags));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los tags");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // GET: api/Tags/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Tag>> GetTag(int id)
        {
            try
            {
                var tag = await _tagsRepository.GetTagById(id);

                if(tag == null)
                {
                    return NotFound(ApiResponse.Failure("No se encontro el Tag"));
                }

                return Ok(tag);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el tag con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // POST: api/Tags
        [HttpPost]
        public async Task<ActionResult<Tag>> CreateTag(TagCreateDTO tagDTO)
        {
            try
            {
                // Verificar si ya existe un tag con el mismo nombre
                var existingTag = await _tagsRepository.BuscarTagPorNombre(tagDTO.Nombre);
                if(existingTag != null)
                {
                    return Conflict(ApiResponse.Failure($"Ya existe un tag con el nombre '{tagDTO.Nombre}'"));
                }

                var tag = new Tag
                {
                    Nombre = tagDTO.Nombre
                };

                var id = await _tagsRepository.CreateTag(tag);
                tag.Id = id;

                return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, ApiResponse<Tag>.Success(tag));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al crear tag");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // PUT: api/Tags/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, TagUpdateDTO tagDTO)
        {
            try
            {
                // Verificar que el tag exista
                var existingTag = await _tagsRepository.GetTagById(id);
                if(existingTag == null)
                {
                    return NotFound(ApiResponse.Failure("El tag no existe"));
                }

                // Verificar si ya existe otro tag con el mismo nombre
                var tagConNombre = await _tagsRepository.BuscarTagPorNombre(tagDTO.Nombre);
                if(tagConNombre != null && tagConNombre.Id != id)
                {
                    return Conflict(ApiResponse.Failure($"Ya existe otro tag con el nombre '{tagDTO.Nombre}'"));
                }

                existingTag.Nombre = tagDTO.Nombre;

                var result = await _tagsRepository.UpdateTag(id, existingTag);

                if(result)
                {
                    return Ok(ApiResponse.Success("El tag se actualizo exitosamente"));
                }
                else
                {
                    return StatusCode(500, ApiResponse.Failure("Error al actualizar el tag"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el tag con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // DELETE: api/Tags/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                // Verificar que el tag exista
                var existingTag = await _tagsRepository.GetTagById(id);
                if(existingTag == null)
                {
                    return NotFound(ApiResponse.Failure("El tag no existe"));
                }

                var result = await _tagsRepository.DeleteTag(id);

                if(result)
                {
                    return Ok(ApiResponse.Success("El tag fue eliminado exitosamente"));
                }
                else
                {
                    return StatusCode(500, ApiResponse.Failure("Error al eliminar el tag"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el tag con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }
    }
}
