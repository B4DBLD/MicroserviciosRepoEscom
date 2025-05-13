using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class TagUpdateDTO
    {
        [Required(ErrorMessage = "El nombre del tag es requerido")]
        public string Nombre { get; set; } = string.Empty;
    }
}
