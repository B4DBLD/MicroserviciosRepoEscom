using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class FavoritoCreateDTO
    {
        [Required(ErrorMessage = "El ID del usuario es requerido")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "El ID del material es requerido")]
        public int MaterialId { get; set; }
    }
}
