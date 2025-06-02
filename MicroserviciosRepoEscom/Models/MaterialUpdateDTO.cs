using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    // DTO para recibir datos del cliente - Con información de autores
    public class MaterialUpdateDTO
    {
        public string? NombreMaterial { get; set; }
        public string? Url { get; set; }
        public List<AutorCreateDTO>? Autores { get; set; }  // Lista de IDs de autores

        public List<int> AutoresIds { get; set; } = new List<int>();
        public List<int> TagIds { get; set; } = new List<int>();
    }
}
