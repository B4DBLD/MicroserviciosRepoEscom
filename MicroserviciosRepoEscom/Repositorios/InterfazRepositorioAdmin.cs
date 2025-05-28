namespace MicroserviciosRepoEscom.Repositorios
{
    public interface InterfazRepositorioAdmin
    {
        Task<bool> CambiarStatus(int materialId, int status);
    }
}
