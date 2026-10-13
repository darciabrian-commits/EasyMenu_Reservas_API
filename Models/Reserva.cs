namespace EasyMenu.Reservas.API.Models
{
    public class Reserva
    {
        public int Id { get; set; }

        public string NombreCliente { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public DateOnly FechaReserva { get; set; }

        public TimeOnly HoraReserva { get; set; }

        public int CantidadPersonas { get; set; }

        public int MesaId { get; set; }

        public string Estado { get; set; } = "PENDIENTE";

        public string? Observaciones { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public Mesa? Mesa { get; set; }
    }
}