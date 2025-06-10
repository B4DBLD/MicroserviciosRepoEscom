namespace MicroserviciosRepoEscom.Models
{
    public class MaterialConRelacionesDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string TipoArchivo { get; set; } = string.Empty;
        public int Disponible { get; set; } = 0;
        public int Status { get; set; } = 0;
        public string? CreadoPor { get; set; }
        public int? CreadorId { get; set; }
        public bool Favorito { get; set; } = false;
        public string FechaCreacion { get; set; } = string.Empty;
        public string FechaActualizacion { get; set; } = string.Empty;
        
        public List<Autor> Autores { get; set; } = new List<Autor>();
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }
}

