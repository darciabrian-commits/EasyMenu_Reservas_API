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
    }
}

