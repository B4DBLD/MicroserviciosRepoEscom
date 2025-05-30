﻿using System.ComponentModel.DataAnnotations;

namespace MicroserviciosRepoEscom.Models
{
    public class DisponibilidadDTO
    {
        [Required]
        [Range(0, 1, ErrorMessage = "Disponible debe ser 0 (deshabilitado) o 1 (habilitado)")]
        public int Disponible { get; set; }
    }

    public class StatusDTO
    {
        [Required]
        [Range(0, 1, ErrorMessage = "Status debe ser 0 (deshabilitado) o 1 (habilitado)")]
        public int Status { get; set; }
    }
}
