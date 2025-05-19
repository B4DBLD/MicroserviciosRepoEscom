namespace MicroserviciosRepoEscom.Servicios
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string nombreMaterial, string autorNombre);
    }
}
