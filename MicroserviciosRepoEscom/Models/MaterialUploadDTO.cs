namespace MicroserviciosRepoEscom.Models
{
    public class MaterialUploadDTO
    {
        public string NombreMaterial { get; set; } = string.Empty;
        public List<AutorCreateDTO> Autores { get; set; } = new List<AutorCreateDTO>();
        public List<int> TagIds { get; set; } = new List<int>();
    }
}
