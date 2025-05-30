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
        // GET: api/Autores/{email}
        [HttpGet("email/{email}")]
        public async Task<ActionResult<Autor>> GetAutorByEmail(string email)
        {
            try
            {
                var autor = await _autoresRepository.GetAutorByEmail(email);

                if (autor == null)
                {
                    return NotFound(ApiResponse.Failure("Autor no encontrado"));
                }

                return Ok(ApiResponse<Autor>.Success(autor));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el autor con Email {email}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        [HttpGet("GetRelacion/{userId}")]
        public async Task<ActionResult<RelacionDTO>> GetRelacion(int userId)
        {
            try
            {
                var autorID = await _autoresRepository.GetRelacion(userId);

                if(autorID == null)
                {
                    return NotFound(ApiResponse.Failure("Este usuario no cuenta con una ID de autor"));
                }

                return Ok(ApiResponse<RelacionDTO>.Success(autorID));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el ID de autor con el ID de usuario: {userId}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        [HttpGet("CreateRelacion/{userID}")]
        public async Task<ActionResult<bool>> CreateRelacion(int userID, int autorID)
        {
            try
            {
                var relacion = await _autoresRepository.CrearRelacion(userID, autorID);

                if (relacion == false)
                {
                    return Conflict(ApiResponse.Failure("No se pudo crear la relación"));
                }

                return Ok(ApiResponse.Success("Se creo la relación exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear la relación entre IDs");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        [HttpDelete("DeleteRelacion")]
        public async Task<ActionResult<bool>> DeleteRelacion(int? userId, int? autorId)
        {
            try
            {
                var relacion = await _autoresRepository.EliminarRelacion(userId, autorId);
                if (relacion == false)
                {
                    return Conflict(ApiResponse.Failure("No se pudo eliminar la relación"));
                }
                return Ok(ApiResponse.Success("Se elimino la relación exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar la relación entre IDs");
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
                    ApellidoP = autorDTO.ApellidoP,
                    ApellidoM = autorDTO.ApellidoM,
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

                if(!string.IsNullOrEmpty(autorDTO.ApellidoP))
                    existingAutor.ApellidoP = autorDTO.ApellidoP;

                if (!string.IsNullOrEmpty(autorDTO.ApellidoM))
                    existingAutor.ApellidoM = autorDTO.ApellidoM;

                if (!string.IsNullOrEmpty(autorDTO.Email))
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
