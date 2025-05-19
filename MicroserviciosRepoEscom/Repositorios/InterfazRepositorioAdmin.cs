namespace MicroserviciosRepoEscom.Repositorios
{
    public interface InterfazRepositorioAdmin
    {
        Task<bool> CambiarDisponibilidad(int id, int disponible);
    }
}
