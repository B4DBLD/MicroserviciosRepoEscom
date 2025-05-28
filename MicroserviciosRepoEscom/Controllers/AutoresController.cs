using MicroserviciosRepoEscom.Models;
using MicroserviciosRepoEscom.Repositorios;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviciosRepoEscom.Controllers
{
    [Route("repositorio/[controller]")]
    [ApiController]
    public class AutoresController : ControllerBase
    {
        private readonly InterfazRepositorioAutores _autoresRepository;
        private readonly ILogger<AutoresController> _logger;

        public AutoresController(InterfazRepositorioAutores autoresRepository, ILogger<AutoresController> logger)
        {
            _autoresRepository = autoresRepository;
            _logger = logger;
        }

        // GET: api/Autores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Autor>>> GetAutores()
        {
            try
            {
                var autores = await _autoresRepository.GetAllAutores();
                return Ok(ApiResponse<IEnumerable<Autor>>.Success(autores));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los autores");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // GET: api/Autores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Autor>> GetAutor(int id)
        {
            try
            {
                var autor = await _autoresRepository.GetAutorById(id);

                if(autor == null)
                {
                    return NotFound();
                }

                return Ok(ApiResponse<Autor>.Success(autor));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el autor con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // GET: api/Autores/5
        [HttpGet("Relacion/{id}")]
        public async Task<ActionResult<int>> GetRelacion(int id)
        {
            try
            {
                var autor = await _autoresRepository.GetAutorById(id);

                if(autor == null)
                {
                    return NotFound();
                }

                return Ok(ApiResponse<Autor>.Success(autor));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el autor con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // POST: api/Autores
        [HttpPost]
        public async Task<ActionResult<Autor>> CreateAutor(AutorCreateDTO autorDTO)
        {
            try
            {
                // Verificar si ya existe un autor con el mismo email
                var existingAutor = await _autoresRepository.GetAutorByEmail(autorDTO.Email);
                if(existingAutor != null)
                {
                    return Conflict(ApiResponse.Failure("Ya existe un autor con este email"));
                }

                var autor = new Autor
                {
                    Nombre = autorDTO.Nombre,
                    Apellido = autorDTO.Apellido,
                    Email = autorDTO.Email
                };

                var id = await _autoresRepository.CreateAutor(autor);
                autor.Id = id;

                return CreatedAtAction(nameof(GetAutor), new { id = autor.Id }, ApiResponse<Autor>.Success(autor));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al crear autor");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // PUT: api/Autores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAutor(int id, AutorUpdateDTO autorDTO)
        {
            try
            {
                // Verificar que el autor exista
                var existingAutor = await _autoresRepository.GetAutorById(id);
                if(existingAutor == null)
                {
                    return NotFound(ApiResponse.Failure("No se encontro el autor"));
                }

                // Verificar si ya existe otro autor con el mismo email
                if(!string.IsNullOrEmpty(autorDTO.Email) && autorDTO.Email != existingAutor.Email)
                {
                    var autorConEmail = await _autoresRepository.GetAutorByEmail(autorDTO.Email);
                    if(autorConEmail != null && autorConEmail.Id != id)
                    {
                        return Conflict(ApiResponse.Failure("Ya existe otro autor con este email"));
                    }
                }

                // Actualizar solo los campos proporcionados
                if(!string.IsNullOrEmpty(autorDTO.Nombre))
                    existingAutor.Nombre = autorDTO.Nombre;

                if(!string.IsNullOrEmpty(autorDTO.Apellido))
                    existingAutor.Apellido = autorDTO.Apellido;

                if(!string.IsNullOrEmpty(autorDTO.Email))
                    existingAutor.Email = autorDTO.Email;

                var result = await _autoresRepository.UpdateAutor(id, existingAutor);

                if(result)
                {
                    return Ok(ApiResponse.Success("Se actualizo correctamente el autor"));
                }
                else
                {
                    return StatusCode(500, ApiResponse.Failure("Error al actualizar el autor"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el autor con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // DELETE: api/Autores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAutor(int id)
        {
            try
            {
                // Verificar que el autor exista
                var existingAutor = await _autoresRepository.GetAutorById(id);
                if(existingAutor == null)
                {
                    return NotFound(ApiResponse.Failure("No se ah encontrado el autor"));
                }

                var result = await _autoresRepository.DeleteAutor(id);

                if(result)
                {
                    return Ok(ApiResponse.Success("El autor se elimino correctamente"));
                }
                else
                {
                    return StatusCode(500, ApiResponse.Failure("Error al eliminar el autor"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el autor con ID {id}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }
    }
}
