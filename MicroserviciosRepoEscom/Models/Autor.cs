using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class Autor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido")]
        public string ApellidoP { get; set; } = string.Empty;

        public string? ApellidoM { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;

        public string FechaCreacion { get; set; } = string.Empty;
        public string FechaActualizacion { get; set; } = string.Empty;
    }
}
