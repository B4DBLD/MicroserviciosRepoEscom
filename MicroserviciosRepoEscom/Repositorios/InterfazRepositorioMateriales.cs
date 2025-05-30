using MicroserviciosRepoEscom.Models;

namespace MicroserviciosRepoEscom.Repositorios
{
    public interface InterfazRepositorioMateriales
    {
            Task<IEnumerable<Material>> GetAllMateriales(int? userRol = null);
            Task<MaterialConRelacionesDTO?> GetMaterialById(int id, int? userRol = null);
            Task<int> CreateMaterial(MaterialCreateDTO material, string? fileUrl = null, string? tipoArchivo = null);
            Task<bool> UpdateMaterial(int id, MaterialUpdateDTO material, string? tipoArchivo = null);
            Task<bool> DeleteMaterial(int id);
            Task<IEnumerable<MaterialConRelacionesDTO>> GetMaterialesByAutorId(int autorId, int? userId = null);
            Task<IEnumerable<MaterialConRelacionesDTO>> GetMaterialesByTagId(int tagId, int? userRol = null);
            Task<IEnumerable<MaterialConRelacionesDTO>> SearchMateriales(string? autorNombre, List<string>? tags);
            Task<IEnumerable<Material>> GetMaterialPorCreador(int userId);
            Task<IEnumerable<MaterialConRelacionesDTO>> SearchMaterialesAvanzado(BusquedaDTO busqueda, int? userRol = null);
            Task<bool> CambiarDisponibilidad(int materialId, int disponible);
            Task<int?> GetUserRol(int? id = null);
    }
}
