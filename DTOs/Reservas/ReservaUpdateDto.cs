using System.ComponentModel.DataAnnotations;

namespace EasyMenu.Reservas.API.DTOs.Reservas
{
    public class ReservaUpdateDto
    {
        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        [MaxLength(120)]
        public string NombreCliente { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Telefono { get; set; }

        public DateOnly FechaReserva { get; set; }

        public TimeOnly HoraReserva { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "La cantidad de personas debe ser mayor que 0.")]
        public int CantidadPersonas { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar una mesa válida.")]
        public int MesaId { get; set; }

        [MaxLength(300)]
        public string? Observaciones { get; set; }
    }
}

