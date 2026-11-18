using EasyMenu.Reservas.API.Data;
using EasyMenu.Reservas.API.Models;
using EasyMenu.Reservas.API.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace EasyMenu.Reservas.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerTestCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}

[Collection(SqlServerTestCollection.Name)]
public sealed class ReservationSqlServerTests
{
    private readonly SqlServerFixture _fixture;

    public ReservationSqlServerTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreacionesConcurrentes_DejanUnaSolaReservaActiva()
    {
        var databaseName = await _fixture.CreateDatabaseAsync();
        var mesaId = await CrearMesaAsync(databaseName, 1);
        var outcomes = await EjecutarConcurrentemente(async _ =>
        {
            await using var context = _fixture.CreateContext(databaseName);
            await new ReservaService(context).CrearAsync(CrearReserva(mesaId));
        });

        Assert.Single(outcomes, succeeded => succeeded);
        Assert.Single(outcomes, succeeded => !succeeded);

        await using var verify = _fixture.CreateContext(databaseName);
        Assert.Equal(1, await verify.Reservas.CountAsync(r => r.Estado != "CANCELADA"));
    }

    [Fact]
    public async Task ActualizacionesConcurrentes_DejanUnaSolaReservaEnElHorario()
    {
        var databaseName = await _fixture.CreateDatabaseAsync();
        var (mesaDestino, mesaOrigenUno, mesaOrigenDos) =
            await CrearMesasAsync(databaseName);
        var fechaObjetivo = DateOnly.FromDateTime(DateTime.Now.AddDays(10));

        int primeraId;
        int segundaId;
        await using (var context = _fixture.CreateContext(databaseName))
        {
            var service = new ReservaService(context);
            var primera = CrearReserva(mesaOrigenUno);
            primera.FechaReserva = fechaObjetivo.AddDays(-2);
            var segunda = CrearReserva(mesaOrigenDos);
            segunda.FechaReserva = fechaObjetivo.AddDays(-1);
            primeraId = (await service.CrearAsync(primera)).Id;
            segundaId = (await service.CrearAsync(segunda)).Id;
        }

        var outcomes = await EjecutarConcurrentemente(async index =>
        {
            await using var context = _fixture.CreateContext(databaseName);
            var datos = CrearReserva(mesaDestino);
            datos.FechaReserva = fechaObjetivo;
            await new ReservaService(context).ActualizarAsync(
                index == 0 ? primeraId : segundaId,
                datos);
        });

        Assert.Single(outcomes, succeeded => succeeded);
        Assert.Single(outcomes, succeeded => !succeeded);

        await using var verify = _fixture.CreateContext(databaseName);
        Assert.Equal(1, await verify.Reservas.CountAsync(r =>
            r.MesaId == mesaDestino &&
            r.FechaReserva == fechaObjetivo &&
            r.HoraReserva == new TimeOnly(18, 0) &&
            r.Estado != "CANCELADA"));
    }

    [Fact]
    public async Task MigracionEIndice_UnHorarioCanceladoPuedeReservarseNuevamente()
    {
        var databaseName = await _fixture.CreateDatabaseAsync();
        var mesaId = await CrearMesaAsync(databaseName, 1);

        await using var context = _fixture.CreateContext(databaseName);
        var service = new ReservaService(context);
        var primera = await service.CrearAsync(CrearReserva(mesaId));
        await service.CambiarEstadoAsync(primera.Id, "CANCELADA");

        var segunda = await service.CrearAsync(CrearReserva(mesaId));

        Assert.NotEqual(primera.Id, segunda.Id);
        Assert.Equal(2, await context.Reservas.CountAsync());
    }

    [Fact]
    public async Task CambiarEstado_DeCanceladaAActivaConConflicto_DevuelveConflicto()
    {
        var databaseName = await _fixture.CreateDatabaseAsync();
        var mesaId = await CrearMesaAsync(databaseName, 1);
        await using var context = _fixture.CreateContext(databaseName);
        var service = new ReservaService(context);

        var primera = await service.CrearAsync(CrearReserva(mesaId));
        await service.CambiarEstadoAsync(primera.Id, "CANCELADA");
        await service.CrearAsync(CrearReserva(mesaId));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CambiarEstadoAsync(primera.Id, "CONFIRMADA"));

        Assert.Contains("reserva", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("CANCELADA",
            (await service.ObtenerPorIdAsync(primera.Id))!.Estado);
    }

    private async Task<int> CrearMesaAsync(string databaseName, int numero)
    {
        await using var context = _fixture.CreateContext(databaseName);
        var mesa = new Mesa
        {
            Numero = numero,
            Capacidad = 4,
            Activa = true,
            FechaCreacion = DateTime.Now
        };
        context.Mesas.Add(mesa);
        await context.SaveChangesAsync();
        return mesa.Id;
    }

    private async Task<(int destino, int origenUno, int origenDos)> CrearMesasAsync(
        string databaseName)
    {
        await using var context = _fixture.CreateContext(databaseName);
        var mesas = new[]
        {
            new Mesa { Numero = 1, Capacidad = 4, Activa = true },
            new Mesa { Numero = 2, Capacidad = 4, Activa = true },
            new Mesa { Numero = 3, Capacidad = 4, Activa = true }
        };
        context.Mesas.AddRange(mesas);
        await context.SaveChangesAsync();
        return (mesas[0].Id, mesas[1].Id, mesas[2].Id);
    }

    private static Reserva CrearReserva(int mesaId)
    {
        return new Reserva
        {
            NombreCliente = "Cliente de integración",
            FechaReserva = DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
            HoraReserva = new TimeOnly(18, 0),
            CantidadPersonas = 2,
            MesaId = mesaId
        };
    }

    private static async Task<bool[]> EjecutarConcurrentemente(Func<int, Task> operation)
    {
        var start = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 2).Select(async index =>
        {
            await start.Task;
            try
            {
                await operation(index);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }).ToArray();

        start.SetResult();
        return await Task.WhenAll(tasks);
    }
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-latest")
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword($"Aa1!{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))}x#")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task<string> CreateDatabaseAsync()
    {
        var databaseName = $"Reservas_{Guid.NewGuid():N}";
        await using var context = CreateContext(databaseName);
        await context.Database.MigrateAsync();
        return databaseName;
    }

    public EasyMenuReservasDbContext CreateContext(string databaseName)
    {
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName,
            Encrypt = true,
            TrustServerCertificate = true
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<EasyMenuReservasDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new EasyMenuReservasDbContext(options);
    }
}
