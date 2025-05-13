using MicroserviciosRepoEscom.Models;

namespace MicroserviciosRepoEscom.Repositorios
{
    public interface InterfazRepositorioMateriales
    {
        Task<IEnumerable<Material>> GetAllMateriales();
        Task<MaterialConRelacionesDTO?> GetMaterialById(int id);
        Task<int> CreateMaterial(MaterialCreateDTO material, string? fileUrl = null, string? tipoArchivo = null);
        Task<bool> UpdateMaterial(int id, MaterialUpdateDTO material);
        Task<bool> DeleteMaterial(int id);
        Task<IEnumerable<MaterialConRelacionesDTO>> GetMaterialesByAutorId(int autorId);
        Task<IEnumerable<MaterialConRelacionesDTO>> GetMaterialesByTagId(int tagId);
        Task<IEnumerable<MaterialConRelacionesDTO>> SearchMateriales(string? autorNombre, List<string>? tags);
        Task<IEnumerable<MaterialConRelacionesDTO>> SearchMaterialesAvanzado(BusquedaDTO busqueda);
    }
}
