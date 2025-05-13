using MicroserviciosRepoEscom.Models;

namespace MicroserviciosRepoEscom.Repositorios
{
    public interface InterfazRepositorioTags
    {
        Task<IEnumerable<Tag>> GetAllTags();
        Task<Tag?> GetTagById(int id);
        Task<int> CreateTag(Tag tag);
        Task<Tag?> BuscarTagPorNombre(string nombre);
        Task<bool> UpdateTag(int id, Tag tag);
        Task<bool> DeleteTag(int id);
    }
}
