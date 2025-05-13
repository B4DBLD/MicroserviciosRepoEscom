namespace MicroserviciosRepoEscom.Servicios
{
    public interface IFileService
    {
        Task<string> SaveFile(IFormFile file);
        Task<byte[]> GetFile(string filePath);
        Task DeleteFile(string filePath);
    }
}
