using EasyMenu.Reservas.API.DTOs.Mesas;
using EasyMenu.Reservas.API.Models;
using EasyMenu.Reservas.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EasyMenu.Reservas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MesasController : ControllerBase
    {
        private readonly MesaService _mesaService;

        public MesasController(MesaService mesaService)
        {
            _mesaService = mesaService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MesaResponseDto>>> ObtenerTodas()
        {
            var mesas = await _mesaService.ObtenerTodasAsync();

            return Ok(mesas.Select(MapearRespuesta));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MesaResponseDto>> ObtenerPorId(int id)
        {
            var mesa = await _mesaService.ObtenerPorIdAsync(id);

            if (mesa == null)
                return NotFound(new { mensaje = "Mesa no encontrada." });

            return Ok(MapearRespuesta(mesa));
        }

        [HttpPost]
        public async Task<ActionResult<MesaResponseDto>> Crear(
            MesaCreateDto dto)
        {
            try
            {
                var mesa = await _mesaService.CrearAsync(
                    dto.Numero,
                    dto.Capacidad);

                var respuesta = MapearRespuesta(mesa);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = mesa.Id },
                    respuesta);
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
        public async Task<ActionResult<MesaResponseDto>> Actualizar(
            int id,
            MesaUpdateDto dto)
        {
            try
            {
                var mesa = await _mesaService.ActualizarAsync(
                    id,
                    dto.Numero,
                    dto.Capacidad,
                    dto.Activa);

                if (mesa == null)
                    return NotFound(new { mensaje = "Mesa no encontrada." });

                return Ok(MapearRespuesta(mesa));
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

        private static MesaResponseDto MapearRespuesta(Mesa mesa)
        {
            return new MesaResponseDto
            {
                Id = mesa.Id,
                Numero = mesa.Numero,
                Capacidad = mesa.Capacidad,
                Activa = mesa.Activa,
                FechaCreacion = mesa.FechaCreacion
            };
        }
    }
}

