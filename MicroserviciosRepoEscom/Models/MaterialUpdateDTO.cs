using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class MaterialUpdateDTO
    {
        public string? Nombre { get; set; }

        [Url(ErrorMessage = "El formato de la URL no es válido")]
        public string? Url { get; set; }

        public List<int>? AutorIds { get; set; }

        public List<int>? TagIds { get; set; }
    }
}
