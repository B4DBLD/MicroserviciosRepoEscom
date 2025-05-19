using MicroserviciosRepoEscom.Models;

namespace MicroserviciosRepoEscom.Repositorios
{
    public interface InterfazRepositorioFavoritos
    {
        Task<bool> AddToFavorites(int userId, int materialId);
        Task<bool> RemoveFromFavorites(int userId, int materialId);
        Task<bool> IsFavorite(int userId, int materialId);
        Task<IEnumerable<MaterialFvoritoDTO>> GetUserFavorites(int userId);
        Task<int> GetFavoritesCount(int materialId);
    }
}
