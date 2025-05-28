using MicroserviciosRepoEscom.Models;
using MicroserviciosRepoEscom.Repositorios;
using MicroserviciosRepoEscom.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviciosRepoEscom.Controllers
{
    [Route("repositorio/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly InterfazRepositorioAdmin _adminRepository;
        private readonly ILogger<MaterialesController> _logger;

        public AdminController(
            InterfazRepositorioMateriales materialesRepository,
            InterfazRepositorioAutores autoresRepository,
            InterfazRepositorioTags tagsRepository,
            IFileService fileService,
            InterfazRepositorioAdmin adminRepository,
            ILogger<MaterialesController> logger)
        {
            _adminRepository = adminRepository;
            _logger = logger;
        }

        [HttpPut("status")]
        public async Task<ActionResult> CambiarStatus(int materialId, [FromBody] StatusDTO Statusdto)
        {
            try
            {

                // Cambiar disponibilidad
                bool result = await _adminRepository.CambiarStatus(materialId, Statusdto.Status);

                if(result)
                {
                    string estadoTexto = Statusdto.Status == 1 ? "Revisado" : "Pendiente";

                    return Ok(ApiResponse<object>.Success(new 
                        { MaterialId = materialId, 
                          StatusDTO = Statusdto.Status}, 
                        $"Material {estadoTexto} exitosamente"));
                }
                else
                {
                    return StatusCode(500, ApiResponse.Failure("Error al cambiar el status del material"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al cambiar el status del material {materialId}");
                return StatusCode(500, ApiResponse.Failure($"Error interno del servidor: {ex.Message}"));
            }
        }
    }
}
