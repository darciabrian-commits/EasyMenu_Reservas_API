using EasyMenu.Reservas.API.Data;
using EasyMenu.Reservas.API.Models;
using EasyMenu.Reservas.API.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyMenu.Reservas.Tests
{
    public class ReservaServiceTests
    {
        private static EasyMenuReservasDbContext CrearContexto()
        {
            var options = new DbContextOptionsBuilder<EasyMenuReservasDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new EasyMenuReservasDbContext(options);
        }

        private static async Task<Mesa> CrearMesaAsync(
            EasyMenuReservasDbContext context,
            bool activa = true,
            int capacidad = 4)
        {
            var mesa = new Mesa
            {
                Numero = 1,
                Capacidad = capacidad,
                Activa = activa,
                FechaCreacion = DateTime.Now
            };

            context.Mesas.Add(mesa);
            await context.SaveChangesAsync();
            return mesa;
        }

        private static Reserva CrearReservaValida(int mesaId)
        {
            return new Reserva
            {
                NombreCliente = "Cliente Prueba",
                Telefono = "7000-0000",
                FechaReserva = DateOnly.FromDateTime(
                    DateTime.Now.AddDays(2)),
                HoraReserva = new TimeOnly(18, 0),
                CantidadPersonas = 2,
                MesaId = mesaId,
                Observaciones = "Prueba"
            };
        }

        [Fact]
        public async Task CrearReservaValida_DebeQuedarPendiente()
        {
            using var context = CrearContexto();
            var mesa = await CrearMesaAsync(context);
            var service = new ReservaService(context);

            var reserva = await service.CrearAsync(
                CrearReservaValida(mesa.Id));

            Assert.Equal("PENDIENTE", reserva.Estado);
            Assert.Equal(1, await context.Reservas.CountAsync());
        }

        [Fact]
        public async Task MesaInactiva_DebeRechazarReserva()
        {
            using var context = CrearContexto();
            var mesa = await CrearMesaAsync(context, activa: false);
            var service = new ReservaService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CrearAsync(
                    CrearReservaValida(mesa.Id)));
        }

        [Fact]
        public async Task PersonasMayorCapacidad_DebeRechazarReserva()
        {
            using var context = CrearContexto();
            var mesa = await CrearMesaAsync(context, capacidad: 2);
            var service = new ReservaService(context);

            var reserva = CrearReservaValida(mesa.Id);
            reserva.CantidadPersonas = 3;

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CrearAsync(reserva));
        }

        [Fact]
        public async Task MismaMesaFechaHora_DebeRechazarSegundaReserva()
        {
            using var context = CrearContexto();
            var mesa = await CrearMesaAsync(context);
            var service = new ReservaService(context);

            await service.CrearAsync(CrearReservaValida(mesa.Id));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CrearAsync(
                    CrearReservaValida(mesa.Id)));
        }

        [Fact]
        public async Task CanceladaNoPuedePasarAAtendida()
        {
            using var context = CrearContexto();
            var mesa = await CrearMesaAsync(context);
            var service = new ReservaService(context);

            var reserva = await service.CrearAsync(
                CrearReservaValida(mesa.Id));

            await service.CambiarEstadoAsync(
                reserva.Id,
                "CANCELADA");

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CambiarEstadoAsync(
                    reserva.Id,
                    "ATENDIDA"));
        }
    }
}

