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
        private readonly InterfazRepositorioMateriales _materialesRepository;
        private readonly InterfazRepositorioAutores _autoresRepository;
        private readonly InterfazRepositorioTags _tagsRepository;
        private readonly IFileService _fileService;
        private readonly InterfazRepositorioAdmin _adminRepository;
        private readonly string _uploadsFolder;
        private readonly ILogger<MaterialesController> _logger;

        public AdminController(
            InterfazRepositorioMateriales materialesRepository,
            InterfazRepositorioAutores autoresRepository,
            InterfazRepositorioTags tagsRepository,
            IFileService fileService,
            InterfazRepositorioAdmin adminRepository,
            ILogger<MaterialesController> logger)
        {
            _materialesRepository = materialesRepository;
            _autoresRepository = autoresRepository;
            _tagsRepository = tagsRepository;
            _fileService = fileService;
            _adminRepository = adminRepository;
            _logger = logger;
        }

        [HttpPut("{id}/disponibilidad")]
        public async Task<ActionResult> CambiarDisponibilidad(int id, [FromBody] DisponibilidadDTO dto)
        {
            try
            {

                // Cambiar disponibilidad
                bool result = await _adminRepository.CambiarDisponibilidad(id, dto.Disponible);

                if(result)
                {
                    string estadoTexto = dto.Disponible == 1 ? "habilitado" : "deshabilitado";

                    return Ok(new
                    {
                        message = $"Material {estadoTexto} exitosamente",
                        materialId = id,
                        disponible = dto.Disponible
                    });
                }
                else
                {
                    return StatusCode(500, "Error al cambiar la disponibilidad del material");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al cambiar disponibilidad del material {id}");
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
