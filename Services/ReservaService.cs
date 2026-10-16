using EasyMenu.Reservas.API.Data;
using EasyMenu.Reservas.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyMenu.Reservas.API.Services
{
    public class ReservaService
    {
        private static readonly string[] EstadosPermitidos =
        {
            "PENDIENTE",
            "CONFIRMADA",
            "ATENDIDA",
            "CANCELADA"
        };

        private readonly EasyMenuReservasDbContext _context;

        public ReservaService(EasyMenuReservasDbContext context)
        {
            _context = context;
        }

        public async Task<List<Reserva>> ObtenerTodasAsync()
        {
            return await _context.Reservas
                .AsNoTracking()
                .Include(r => r.Mesa)
                .OrderBy(r => r.FechaReserva)
                .ThenBy(r => r.HoraReserva)
                .ToListAsync();
        }

        public async Task<Reserva?> ObtenerPorIdAsync(int id)
        {
            return await _context.Reservas
                .AsNoTracking()
                .Include(r => r.Mesa)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Reserva>> ObtenerPorFechaAsync(DateOnly fecha)
        {
            return await _context.Reservas
                .AsNoTracking()
                .Include(r => r.Mesa)
                .Where(r => r.FechaReserva == fecha)
                .OrderBy(r => r.HoraReserva)
                .ToListAsync();
        }

        public async Task<Reserva> CrearAsync(Reserva nuevaReserva)
        {
            await ValidarReservaAsync(nuevaReserva, null);

            nuevaReserva.Estado = "PENDIENTE";
            nuevaReserva.FechaCreacion = DateTime.Now;

            _context.Reservas.Add(nuevaReserva);
            await _context.SaveChangesAsync();

            return await ObtenerPorIdAsync(nuevaReserva.Id)
                ?? nuevaReserva;
        }

        public async Task<Reserva?> ActualizarAsync(
            int id,
            Reserva datosActualizados)
        {
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null)
                return null;

            datosActualizados.Id = id;
            await ValidarReservaAsync(datosActualizados, id);

            reserva.NombreCliente = datosActualizados.NombreCliente;
            reserva.Telefono = datosActualizados.Telefono;
            reserva.FechaReserva = datosActualizados.FechaReserva;
            reserva.HoraReserva = datosActualizados.HoraReserva;
            reserva.CantidadPersonas = datosActualizados.CantidadPersonas;
            reserva.MesaId = datosActualizados.MesaId;
            reserva.Observaciones = datosActualizados.Observaciones;

            await _context.SaveChangesAsync();

            return await ObtenerPorIdAsync(id);
        }

        public async Task<Reserva?> CambiarEstadoAsync(
            int id,
            string nuevoEstado)
        {
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null)
                return null;

            nuevoEstado = nuevoEstado.Trim().ToUpperInvariant();

            if (!EstadosPermitidos.Contains(nuevoEstado))
            {
                throw new ArgumentException(
                    "Estado inválido. Use PENDIENTE, CONFIRMADA, ATENDIDA o CANCELADA.");
            }

            if (reserva.Estado == "CANCELADA" &&
                nuevoEstado == "ATENDIDA")
            {
                throw new InvalidOperationException(
                    "Una reserva cancelada no puede pasar a ATENDIDA.");
            }

            reserva.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            return await ObtenerPorIdAsync(id);
        }

        private async Task ValidarReservaAsync(
            Reserva reserva,
            int? reservaIdExcluir)
        {
            if (string.IsNullOrWhiteSpace(reserva.NombreCliente))
                throw new ArgumentException(
                    "El nombre del cliente es obligatorio.");

            if (reserva.CantidadPersonas <= 0)
                throw new ArgumentException(
                    "La cantidad de personas debe ser mayor que 0.");

            var fechaHora = reserva.FechaReserva
                .ToDateTime(reserva.HoraReserva);

            if (fechaHora <= DateTime.Now)
                throw new ArgumentException(
                    "La fecha y hora de la reserva deben ser futuras.");

            var mesa = await _context.Mesas
                .FirstOrDefaultAsync(m => m.Id == reserva.MesaId);

            if (mesa == null)
                throw new InvalidOperationException(
                    "La mesa seleccionada no existe.");

            if (!mesa.Activa)
                throw new InvalidOperationException(
                    "La mesa seleccionada está inactiva.");

            if (reserva.CantidadPersonas > mesa.Capacidad)
            {
                throw new InvalidOperationException(
                    $"La mesa solo tiene capacidad para {mesa.Capacidad} personas.");
            }

            bool conflicto = await _context.Reservas.AnyAsync(r =>
                r.MesaId == reserva.MesaId &&
                r.FechaReserva == reserva.FechaReserva &&
                r.HoraReserva == reserva.HoraReserva &&
                r.Estado != "CANCELADA" &&
                (!reservaIdExcluir.HasValue ||
                 r.Id != reservaIdExcluir.Value));

            if (conflicto)
            {
                throw new InvalidOperationException(
                    "La mesa ya tiene una reserva en esa fecha y hora.");
            }
        }
    }
}