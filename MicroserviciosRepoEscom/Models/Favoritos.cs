namespace MicroserviciosRepoEscom.Models
{
    public class Favoritos
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MaterialId { get; set; }
        public string FechaAgregado { get; set; } = string.Empty;
    }
}
