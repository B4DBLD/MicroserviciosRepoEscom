using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class AutorUpdateDTO
    {
        public string? Nombre { get; set; }

        public string? Apellido { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string? Email { get; set; }
    }
}
