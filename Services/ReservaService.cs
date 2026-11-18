using EasyMenu.Reservas.API.Data;
using EasyMenu.Reservas.API.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
            await using var transaction = await IniciarTransaccionSerializableAsync();

            try
            {
                await ValidarReservaAsync(nuevaReserva, null);

                nuevaReserva.Estado = "PENDIENTE";
                nuevaReserva.FechaCreacion = DateTime.Now;

                _context.Reservas.Add(nuevaReserva);
                await _context.SaveChangesAsync();
                if (transaction is not null)
                    await transaction.CommitAsync();

                return await ObtenerPorIdAsync(nuevaReserva.Id)
                    ?? nuevaReserva;
            }
            catch (Exception ex) when (EsConflictoConcurrente(ex))
            {
                throw CrearErrorConflicto(ex);
            }
        }

        public async Task<Reserva?> ActualizarAsync(
            int id,
            Reserva datosActualizados)
        {
            await using var transaction = await IniciarTransaccionSerializableAsync();

            try
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
                if (transaction is not null)
                    await transaction.CommitAsync();

                return await ObtenerPorIdAsync(id);
            }
            catch (Exception ex) when (EsConflictoConcurrente(ex))
            {
                throw CrearErrorConflicto(ex);
            }
        }

        public async Task<Reserva?> CambiarEstadoAsync(
            int id,
            string nuevoEstado)
        {
            await using var transaction = await IniciarTransaccionSerializableAsync();

            try
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

                if (reserva.Estado == "CANCELADA" &&
                    nuevoEstado != "CANCELADA")
                {
                    await ValidarReservaAsync(reserva, id);
                }

                reserva.Estado = nuevoEstado;
                await _context.SaveChangesAsync();
                if (transaction is not null)
                    await transaction.CommitAsync();

                return await ObtenerPorIdAsync(id);
            }
            catch (Exception ex) when (EsConflictoConcurrente(ex))
            {
                throw CrearErrorConflicto(ex);
            }
        }

        private async Task ValidarReservaAsync(
            Reserva reserva,
            int? reservaIdExcluir)
        {
            if (string.IsNullOrWhiteSpace(reserva.NombreCliente))
                throw new ArgumentException(
                    "El nombre del cliente es obligatorio.");

            if (reserva.NombreCliente.Length > 120)
                throw new ArgumentException(
                    "El nombre del cliente no puede superar 120 caracteres.");

            if (reserva.Telefono?.Length > 30)
                throw new ArgumentException(
                    "El teléfono no puede superar 30 caracteres.");

            if (reserva.Observaciones?.Length > 300)
                throw new ArgumentException(
                    "Las observaciones no pueden superar 300 caracteres.");

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

        private async Task<IDbContextTransaction?> IniciarTransaccionSerializableAsync()
        {
            return _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable)
                : null;
        }

        private static bool EsConflictoConcurrente(Exception exception)
        {
            return ObtenerSqlException(exception)?.Number is 1205 or 2601 or 2627;
        }

        private static InvalidOperationException CrearErrorConflicto(Exception exception)
        {
            var mensaje = ObtenerSqlException(exception)?.Number == 1205
                ? "La reserva entró en conflicto con otra operación concurrente. Intente nuevamente."
                : "La mesa ya tiene una reserva en esa fecha y hora.";

            return new InvalidOperationException(mensaje);
        }

        private static SqlException? ObtenerSqlException(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is SqlException sqlException)
                    return sqlException;
            }

            return null;
        }
    }
}