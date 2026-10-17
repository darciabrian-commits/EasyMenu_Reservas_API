using EasyMenu.Reservas.API.Data;
using EasyMenu.Reservas.API.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyMenu.Reservas.Tests
{
    public class MesaServiceTests
    {
        private static EasyMenuReservasDbContext CrearContexto()
        {
            var options = new DbContextOptionsBuilder<EasyMenuReservasDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new EasyMenuReservasDbContext(options);
        }

        [Fact]
        public async Task CrearMesaValida_DebeGuardarMesa()
        {
            using var context = CrearContexto();
            var service = new MesaService(context);

            var mesa = await service.CrearAsync(1, 4);

            Assert.Equal(1, mesa.Numero);
            Assert.Equal(4, mesa.Capacidad);
            Assert.True(mesa.Activa);
            Assert.Equal(1, await context.Mesas.CountAsync());
        }

        [Fact]
        public async Task CrearMesaCapacidadCero_DebeLanzarError()
        {
            using var context = CrearContexto();
            var service = new MesaService(context);

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CrearAsync(1, 0));
        }

        [Fact]
        public async Task CrearNumeroDuplicado_DebeLanzarError()
        {
            using var context = CrearContexto();
            var service = new MesaService(context);

            await service.CrearAsync(1, 4);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CrearAsync(1, 6));
        }

        [Fact]
        public async Task ObtenerTodas_DebeRetornarMesasOrdenadasPorNumero()
        {
            using var context = CrearContexto();
            var service = new MesaService(context);

            await service.CrearAsync(3, 4);
            await service.CrearAsync(1, 2);
            await service.CrearAsync(2, 6);

            var mesas = await service.ObtenerTodasAsync();

            Assert.Equal(3, mesas.Count);
            Assert.Equal(1, mesas[0].Numero);
            Assert.Equal(2, mesas[1].Numero);
            Assert.Equal(3, mesas[2].Numero);
        }

        [Fact]
        public async Task ObtenerPorIdExistente_DebeRetornarMesa()
        {
            using var context = CrearContexto();
            var service = new MesaService(context);

            var creada = await service.CrearAsync(5, 4);

            var mesa = await service.ObtenerPorIdAsync(creada.Id);

            Assert.NotNull(mesa);
            Assert.Equal(5, mesa.Numero);
            Assert.Equal(4, mesa.Capacidad);
        }

        [Fact]
        public async Task ObtenerPorIdInexistente_DebeRetornarNull()
        {
            using var context = CrearContexto();
            var service = new MesaService(context);

            var mesa = await service.ObtenerPorIdAsync(999);

            Assert.Null(mesa);
        }

        [Fact]
        public async Task ActualizarMesaValida_DebeModificarDatos()
        {
            using var context = CrearContexto();
            var service = new MesaService(context);

            var creada = await service.CrearAsync(1, 4);

            var actualizada = await service.ActualizarAsync(
                creada.Id,
                2,
                6,
                false);

            Assert.NotNull(actualizada);
            Assert.Equal(2, actualizada.Numero);
            Assert.Equal(6, actualizada.Capacidad);
            Assert.False(actualizada.Activa);
        }
    }
}
