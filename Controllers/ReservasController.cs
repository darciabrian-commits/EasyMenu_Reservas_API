using EasyMenu.Reservas.API.DTOs.Reservas;
using EasyMenu.Reservas.API.Models;
using EasyMenu.Reservas.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EasyMenu.Reservas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservasController : ControllerBase
    {
        private readonly ReservaService _reservaService;

        public ReservasController(ReservaService reservaService)
        {
            _reservaService = reservaService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservaResponseDto>>> ObtenerTodas()
        {
            var reservas = await _reservaService.ObtenerTodasAsync();
            return Ok(reservas.Select(MapearRespuesta));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReservaResponseDto>> ObtenerPorId(int id)
        {
            var reserva = await _reservaService.ObtenerPorIdAsync(id);

            if (reserva == null)
                return NotFound(new { mensaje = "Reserva no encontrada." });

            return Ok(MapearRespuesta(reserva));
        }

        [HttpGet("fecha/{fecha}")]
        public async Task<ActionResult<IEnumerable<ReservaResponseDto>>> ObtenerPorFecha(
            DateOnly fecha)
        {
            var reservas = await _reservaService.ObtenerPorFechaAsync(fecha);
            return Ok(reservas.Select(MapearRespuesta));
        }

        [HttpPost]
        public async Task<ActionResult<ReservaResponseDto>> Crear(
            ReservaCreateDto dto)
        {
            try
            {
                var nueva = new Reserva
                {
                    NombreCliente = dto.NombreCliente.Trim(),
                    Telefono = dto.Telefono,
                    FechaReserva = dto.FechaReserva,
                    HoraReserva = dto.HoraReserva,
                    CantidadPersonas = dto.CantidadPersonas,
                    MesaId = dto.MesaId,
                    Observaciones = dto.Observaciones
                };

                var reserva = await _reservaService.CrearAsync(nueva);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = reserva.Id },
                    MapearRespuesta(reserva));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ReservaResponseDto>> Actualizar(
            int id,
            ReservaUpdateDto dto)
        {
            try
            {
                var datos = new Reserva
                {
                    NombreCliente = dto.NombreCliente.Trim(),
                    Telefono = dto.Telefono,
                    FechaReserva = dto.FechaReserva,
                    HoraReserva = dto.HoraReserva,
                    CantidadPersonas = dto.CantidadPersonas,
                    MesaId = dto.MesaId,
                    Observaciones = dto.Observaciones
                };

                var reserva = await _reservaService
                    .ActualizarAsync(id, datos);

                if (reserva == null)
                    return NotFound(new { mensaje = "Reserva no encontrada." });

                return Ok(MapearRespuesta(reserva));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<ActionResult<ReservaResponseDto>> CambiarEstado(
            int id,
            ReservaEstadoDto dto)
        {
            try
            {
                var reserva = await _reservaService
                    .CambiarEstadoAsync(id, dto.Estado);

                if (reserva == null)
                    return NotFound(new { mensaje = "Reserva no encontrada." });

                return Ok(MapearRespuesta(reserva));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }

        private static ReservaResponseDto MapearRespuesta(Reserva reserva)
        {
            return new ReservaResponseDto
            {
                Id = reserva.Id,
                NombreCliente = reserva.NombreCliente,
                Telefono = reserva.Telefono,
                FechaReserva = reserva.FechaReserva,
                HoraReserva = reserva.HoraReserva,
                CantidadPersonas = reserva.CantidadPersonas,
                MesaId = reserva.MesaId,
                NumeroMesa = reserva.Mesa?.Numero,
                Estado = reserva.Estado,
                Observaciones = reserva.Observaciones,
                FechaCreacion = reserva.FechaCreacion
            };
        }
    }
}
