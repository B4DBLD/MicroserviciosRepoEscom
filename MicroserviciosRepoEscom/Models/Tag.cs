using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del tag es requerido")]
        public string Nombre { get; set; } = string.Empty;
    }
}
