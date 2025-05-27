using MicroserviciosRepoEscom.Models;

namespace MicroserviciosRepoEscom.Repositorios
{
    public interface InterfazRepositorioHistorial
    {
        Task<bool> RegistrarConsulta(int userId, int materialId);
        Task<IEnumerable<UserSearch>> GetHistorialUsuario(int userId);
        Task<bool> EliminarMaterialDelHistorial(int userId, int materialId);
        Task<bool> LimpiarTodoElHistorial(int userId);
    }
}
