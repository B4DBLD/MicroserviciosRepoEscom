using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class MaterialCreateDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; } = string.Empty;

        // Lista de IDs de autores
        public List<int>? AutorIds { get; set; }

        // Lista de IDs de tags
        public List<int>? TagIds { get; set; }

        // Campo para subir archivo
        public IFormFile? Archivo { get; set; }

        // Nombres de autores para crear o buscar
        public string? NombresAutores { get; set; }

        // Nombres de tags para crear o buscar
        public string? NombresTags { get; set; }

    }
}
