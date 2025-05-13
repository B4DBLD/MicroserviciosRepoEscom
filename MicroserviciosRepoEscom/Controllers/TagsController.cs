using MicroserviciosRepoEscom.Models;
using MicroserviciosRepoEscom.Repositorios;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviciosRepoEscom.Controllers
{
    [Route("api/[controller]")]
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
                return Ok(tags);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los tags");
                return StatusCode(500, "Error interno del servidor");
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
                    return NotFound();
                }

                return Ok(tag);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el tag con ID {id}");
                return StatusCode(500, "Error interno del servidor");
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
                    return Conflict($"Ya existe un tag con el nombre '{tagDTO.Nombre}'");
                }

                var tag = new Tag
                {
                    Nombre = tagDTO.Nombre
                };

                var id = await _tagsRepository.CreateTag(tag);
                tag.Id = id;

                return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al crear tag");
                return StatusCode(500, "Error interno del servidor");
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
                    return NotFound();
                }

                // Verificar si ya existe otro tag con el mismo nombre
                var tagConNombre = await _tagsRepository.BuscarTagPorNombre(tagDTO.Nombre);
                if(tagConNombre != null && tagConNombre.Id != id)
                {
                    return Conflict($"Ya existe otro tag con el nombre '{tagDTO.Nombre}'");
                }

                existingTag.Nombre = tagDTO.Nombre;

                var result = await _tagsRepository.UpdateTag(id, existingTag);

                if(result)
                {
                    return NoContent();
                }
                else
                {
                    return StatusCode(500, "Error al actualizar el tag");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el tag con ID {id}");
                return StatusCode(500, "Error interno del servidor");
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
                    return NotFound();
                }

                var result = await _tagsRepository.DeleteTag(id);

                if(result)
                {
                    return NoContent();
                }
                else
                {
                    return StatusCode(500, "Error al eliminar el tag");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el tag con ID {id}");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}
