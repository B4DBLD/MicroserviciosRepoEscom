namespace MicroserviciosRepoEscom.Models
{
    public class MaterialConRelacionesDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string TipoArchivo { get; set; } = string.Empty;
        public string rutaAcceso { get; set; } = string.Empty;
        public string FechaCreacion { get; set; } = string.Empty;
        public string FechaActualizacion { get; set; } = string.Empty;
        public List<Autor> Autores { get; set; } = new List<Autor>();
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }
}
