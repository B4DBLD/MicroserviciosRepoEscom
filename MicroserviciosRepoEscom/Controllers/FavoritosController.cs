﻿using MicroserviciosRepoEscom.Models;
using MicroserviciosRepoEscom.Repositorios;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviciosRepoEscom.Controllers
{
    [Route("repositorio/[controller]")]
    [ApiController]
    public class FavoritosController : ControllerBase
    {
        private readonly InterfazRepositorioFavoritos _favoritosRepository;
        private readonly InterfazRepositorioMateriales _materialesRepository;
        private readonly ILogger<FavoritosController> _logger;

        public FavoritosController(
            InterfazRepositorioFavoritos favoritosRepository,
            InterfazRepositorioMateriales materialesRepository,
            ILogger<FavoritosController> logger)
        {
            _favoritosRepository = favoritosRepository;
            _materialesRepository = materialesRepository;
            _logger = logger;
        }

        [HttpPost("Agregar")]
        public async Task<ActionResult<FavoritoRespuesta>> AddToFavorites([FromBody] FavoritoCreateDTO favoritoDTO)
        {
            try
            {
                int? userRol = await _materialesRepository.GetUserRol(favoritoDTO.UserId);
                // Verificar que el material existe
                var material = await _materialesRepository.GetMaterialById(favoritoDTO.MaterialId, userRol);
                if(material == null)
                {
                    return NotFound(ApiResponse.Failure($"No se encontró el material con ID {favoritoDTO.MaterialId}"));
                }

                // Agregar a favoritos
                bool result = await _favoritosRepository.AddToFavorites(favoritoDTO.UserId, favoritoDTO.MaterialId);

                if(result)
                {
                    return Ok(ApiResponse.Success("Material agregado a favoritos exitosamente"));
                }
                else
                {
                    return BadRequest(ApiResponse.Success($"El material ya está en favoritos"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al agregar material {favoritoDTO.MaterialId} a favoritos del usuario {favoritoDTO.UserId}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // DELETE: api/Favoritos/{userId}/{materialId}
        [HttpDelete("{userId}/{materialId}")]
        public async Task<ActionResult<FavoritoRespuesta>> RemoveFromFavorites(int userId, int materialId)
        {
            try
            {
                bool result = await _favoritosRepository.RemoveFromFavorites(userId, materialId);

                if(result)
                {
                    return Ok(ApiResponse.Success("Material removido de favoritos exitosamente"));
                }
                else
                {
                    return NotFound(ApiResponse.Failure("El material no estaba en favoritos"));
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al remover material {materialId} de favoritos del usuario {userId}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }


        // GET: api/Favoritos/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<MaterialFvoritoDTO>>> GetUserFavorites(int userId)
        {
            try
            {
                var favoritos = await _favoritosRepository.GetUserFavorites(userId);
                return Ok(ApiResponse<IEnumerable<MaterialFvoritoDTO>>.Success (favoritos));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener favoritos del usuario {userId}");
                return StatusCode(500, ApiResponse.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

    }
}
