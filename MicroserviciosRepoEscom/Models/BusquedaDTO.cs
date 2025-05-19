namespace MicroserviciosRepoEscom.Models
{
    public class BusquedaDTO
    {
        public string? MaterialNombre { get; set; }
        public string? AutorNombre { get; set; }
        public List<int>? Tags { get; set; }
        public int? UserRol { get; set; } // 1 = Alumno, 3 = Admin
    }
}
