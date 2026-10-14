using System.ComponentModel.DataAnnotations;

namespace EasyMenu.Reservas.API.DTOs.Reservas
{
    public class ReservaEstadoDto
    {
        [Required]
        public string Estado { get; set; } = string.Empty;
    }
}

