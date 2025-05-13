namespace MicroserviciosRepoEscom.Servicios
{
    public class FileService : IFileService
    {
        private readonly string _uploadsFolder;
        private readonly ILogger<FileService> _logger;

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _uploadsFolder = configuration["FileStorage:UploadsFolder"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            _logger = logger;

            // Asegurar que el directorio existe
            if(!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        public async Task<string> SaveFile(IFormFile file)
        {
            try
            {
                // Crear un nombre único para el archivo
                string fileExtension = Path.GetExtension(file.FileName);
                string fileName = $"{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(_uploadsFolder, fileName);

                using(var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Devuelve la ruta relativa para guardar en la base de datos
                return fileName;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al guardar el archivo");
                throw;
            }
        }

        public async Task<byte[]> GetFile(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_uploadsFolder, fileName);

                if(!File.Exists(filePath))
                {
                    throw new FileNotFoundException("El archivo no existe", fileName);
                }

                return await File.ReadAllBytesAsync(filePath);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el archivo {fileName}");
                throw;
            }
        }

        public async Task DeleteFile(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_uploadsFolder, fileName);

                if(File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await Task.CompletedTask;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el archivo {fileName}");
                throw;
            }
        }
    }
}
