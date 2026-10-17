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

        [Fact]
        public async Task ObtenerTodas_DebeRetornarReservasOrdenadas()
        {
            using var context = CrearContexto();
            var mesa = await CrearMesaAsync(context);
            var service = new ReservaService(context);

            var primera = CrearReservaValida(mesa.Id);
            primera.FechaReserva = DateOnly.FromDateTime(DateTime.Now.AddDays(3));
            primera.HoraReserva = new TimeOnly(19, 0);

            var segunda = CrearReservaValida(mesa.Id);
            segunda.FechaReserva = DateOnly.FromDateTime(DateTime.Now.AddDays(2));
            segunda.HoraReserva = new TimeOnly(18, 0);

            await service.CrearAsync(primera);
            await service.CrearAsync(segunda);

            var reservas = await service.ObtenerTodasAsync();

            Assert.Equal(2, reservas.Count);
            Assert.Equal(segunda.FechaReserva, reservas[0].FechaReserva);
            Assert.Equal(primera.FechaReserva, reservas[1].FechaReserva);
        }

        [Fact]
        public async Task ObtenerPorIdExistente_DebeRetornarReserva()
        {
            using var context = CrearContexto();
            var mesa = await CrearMesaAsync(context);
            var service = new ReservaService(context);

            var creada = await service.CrearAsync(CrearReservaValida(mesa.Id));

            var reserva = await service.ObtenerPorIdAsync(creada.Id);

            Assert.NotNull(reserva);
            Assert.Equal(creada.Id, reserva.Id);
            Assert.Equal("Cliente Prueba", reserva.NombreCliente);
        }

        [Fact]
        public async Task ObtenerPorFecha_DebeRetornarSoloLaFechaSolicitada()
        {
            using var context = CrearContexto();
            var mesa = await CrearMesaAsync(context);
            var service = new ReservaService(context);

            var fechaBuscada = DateOnly.FromDateTime(DateTime.Now.AddDays(2));

            var reserva1 = CrearReservaValida(mesa.Id);
            reserva1.FechaReserva = fechaBuscada;
            reserva1.HoraReserva = new TimeOnly(18, 0);

            var reserva2 = CrearReservaValida(mesa.Id);
            reserva2.FechaReserva = DateOnly.FromDateTime(DateTime.Now.AddDays(3));
            reserva2.HoraReserva = new TimeOnly(19, 0);

            await service.CrearAsync(reserva1);
            await service.CrearAsync(reserva2);

            var resultado = await service.ObtenerPorFechaAsync(fechaBuscada);

            Assert.Single(resultado);
            Assert.Equal(fechaBuscada, resultado[0].FechaReserva);
        }

        [Fact]
        public async Task ActualizarReservaValida_DebeModificarDatos()
        {
            using var context = CrearContexto();
            var mesa = await CrearMesaAsync(context);
            var service = new ReservaService(context);

            var creada = await service.CrearAsync(CrearReservaValida(mesa.Id));

            var cambios = CrearReservaValida(mesa.Id);
            cambios.NombreCliente = "Cliente Actualizado";
            cambios.Telefono = "7111-1111";
            cambios.FechaReserva = DateOnly.FromDateTime(DateTime.Now.AddDays(4));
            cambios.HoraReserva = new TimeOnly(19, 30);
            cambios.CantidadPersonas = 3;
            cambios.Observaciones = "Actualizada";

            var actualizada = await service.ActualizarAsync(creada.Id, cambios);

            Assert.NotNull(actualizada);
            Assert.Equal("Cliente Actualizado", actualizada.NombreCliente);
            Assert.Equal("7111-1111", actualizada.Telefono);
            Assert.Equal(3, actualizada.CantidadPersonas);
            Assert.Equal("Actualizada", actualizada.Observaciones);
        }
        
        [Fact]
public async Task NombreClienteVacio_DebeRechazarReserva()
{
    using var context = CrearContexto();
    var mesa = await CrearMesaAsync(context);
    var service = new ReservaService(context);

    var reserva = CrearReservaValida(mesa.Id);
    reserva.NombreCliente = "";

    await Assert.ThrowsAsync<ArgumentException>(
        () => service.CrearAsync(reserva));
}

[Fact]
public async Task CantidadPersonasCero_DebeRechazarReserva()
{
    using var context = CrearContexto();
    var mesa = await CrearMesaAsync(context);
    var service = new ReservaService(context);

    var reserva = CrearReservaValida(mesa.Id);
    reserva.CantidadPersonas = 0;

    await Assert.ThrowsAsync<ArgumentException>(
        () => service.CrearAsync(reserva));
}

[Fact]
public async Task FechaPasada_DebeRechazarReserva()
{
    using var context = CrearContexto();
    var mesa = await CrearMesaAsync(context);
    var service = new ReservaService(context);

    var reserva = CrearReservaValida(mesa.Id);
    reserva.FechaReserva = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));

    await Assert.ThrowsAsync<ArgumentException>(
        () => service.CrearAsync(reserva));
}

[Fact]
public async Task MesaInexistente_DebeRechazarReserva()
{
    using var context = CrearContexto();
    var service = new ReservaService(context);

    var reserva = CrearReservaValida(999);

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.CrearAsync(reserva));
}

[Fact]
public async Task CambiarEstadoValido_DebeActualizarEstado()
{
    using var context = CrearContexto();
    var mesa = await CrearMesaAsync(context);
    var service = new ReservaService(context);

    var creada = await service.CrearAsync(CrearReservaValida(mesa.Id));
    var actualizada = await service.CambiarEstadoAsync(creada.Id, "confirmada");

    Assert.NotNull(actualizada);
    Assert.Equal("CONFIRMADA", actualizada.Estado);
}

    }
}
