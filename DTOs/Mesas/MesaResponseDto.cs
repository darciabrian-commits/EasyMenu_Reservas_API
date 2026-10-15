namespace EasyMenu.Reservas.API.DTOs.Mesas
{
    public class MesaResponseDto
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public int Capacidad { get; set; }
        public bool Activa { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
