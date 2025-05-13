using MicroserviciosRepoEscom.Models;

namespace MicroserviciosRepoEscom.Repositorios
{
    public interface InterfazRepositorioAutores
    {
        Task<IEnumerable<Autor>> GetAllAutores();
        Task<Autor?> GetAutorById(int id);
        Task<Autor?> BuscarAutorPorNombreApellido(string nombre, string apellido);
        Task<Autor?> GetAutorByEmail(string email);
        Task<int> CreateAutor(Autor autor);
        Task<bool> UpdateAutor(int id, Autor autor);
        Task<bool> DeleteAutor(int id);
    }
}
