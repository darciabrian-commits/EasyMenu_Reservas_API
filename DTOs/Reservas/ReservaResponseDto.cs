namespace EasyMenu.Reservas.API.DTOs.Reservas
{
    public class ReservaResponseDto
    {
        public int Id { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public DateOnly FechaReserva { get; set; }
        public TimeOnly HoraReserva { get; set; }
        public int CantidadPersonas { get; set; }
        public int MesaId { get; set; }
        public int? NumeroMesa { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}

