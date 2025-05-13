using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class Material
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La URL es requerida")]
        public string Url { get; set; } = string.Empty;

        // Nuevo campo para determinar el tipo de archivo (PDF o ZIP)
        public string TipoArchivo { get; set; } = string.Empty;
        public string? RutaAcceso { get; set; } // Nueva propiedad para path/url específico

        public string FechaCreacion { get; set; } = string.Empty;
        public string FechaActualizacion { get; set; } = string.Empty;
    }
}
