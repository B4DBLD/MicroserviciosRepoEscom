using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class AutorUpdateDTO
    {
        public string? Nombre { get; set; }

        public string? ApellidoP { get; set; }
        public string? ApellidoM { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string? Email { get; set; }
    }
}
