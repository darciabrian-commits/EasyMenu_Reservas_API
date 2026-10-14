using System.ComponentModel.DataAnnotations;

namespace EasyMenu.Reservas.API.DTOs.Mesas
{
    public class MesaUpdateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "El número de mesa debe ser mayor que 0.")]
        public int Numero { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La capacidad debe ser mayor que 0.")]
        public int Capacidad { get; set; }

        public bool Activa { get; set; }
    }
}

