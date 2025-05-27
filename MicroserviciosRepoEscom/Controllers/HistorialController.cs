using MicroserviciosRepoEscom.Models;
using MicroserviciosRepoEscom.Repositorios;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviciosRepoEscom.Controllers
{
    [Route("repositorio/[controller]")]
    [ApiController]
    public class HistorialController : ControllerBase
    {
        private readonly InterfazRepositorioHistorial _historialRepository;
        private readonly ILogger<HistorialController> _logger;

        public HistorialController(
            InterfazRepositorioHistorial historialRepository,
            ILogger<HistorialController> logger)
        {
            _historialRepository = historialRepository;
            _logger = logger;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<UserSearch>>> GetHistorial(int userId)
        {
            try
            {
                var historial = await _historialRepository.GetHistorialUsuario(userId);
                return Ok(ApiResponse<IEnumerable<UserSearch>>.Success(historial));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener historial del usuario {userId}");
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // DELETE: api/Historial/{userId}/material/{materialId}
        [HttpDelete("{userId}/material/{materialId}")]
        public async Task<ActionResult> EliminarMaterialDelHistorial(int userId, int materialId)
        {
            try
            {
                bool resultado = await _historialRepository.EliminarMaterialDelHistorial(userId, materialId);

                if (resultado)
                {
                    return Ok(ApiResponse.Success("Material eliminado del historial"));
                }
                else
                {
                    return NotFound(ApiResponse.Failure("Material no encontrado en el historial"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar material del historial: Usuario {userId}, Material {materialId}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // DELETE: api/Historial/{userId}
        [HttpDelete("{userId}")]
        public async Task<ActionResult> LimpiarHistorialCompleto(int userId)
        {
            try
            {
                bool resultado = await _historialRepository.LimpiarTodoElHistorial(userId);

                if (resultado)
                {
                    return Ok(ApiResponse.Success("Historial limpiado completamente"));
                }
                else
                {
                    return NotFound(ApiResponse.Failure("No hay historial para limpiar"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al limpiar historial completo del usuario {userId}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }
    }
}
