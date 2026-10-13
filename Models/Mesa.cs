namespace EasyMenu.Reservas.API.Models
{
    public class Mesa
    {
        public int Id { get; set; }

        public int Numero { get; set; }

        public int Capacidad { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}