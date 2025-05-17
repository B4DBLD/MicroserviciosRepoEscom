using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class MaterialBlobDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        public string NombreMaterial { get; set; } = string.Empty;

        public List<int>? AutorIds { get; set; }

        public List<int>? TagIds { get; set; }

        public string TipoArchivo { get; set; } = string.Empty;

        public string? Url { get; set; } // Para archivos ZIP
    }
}
