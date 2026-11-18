using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net.Http.Json;
using EasyMenu.Reservas.API.Data;
using EasyMenu.Reservas.API.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace EasyMenu.Reservas.Tests;

public class ApiAuthorizationTests
{
    [Fact]
    public async Task SinCredenciales_ConsultaDeDatosPersonalesDevuelve401()
    {
        using var factory = new ReservasApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Reservas");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SinCredenciales_CrearMesaDevuelve401()
    {
        using var factory = new ReservasApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/Mesas",
            new { numero = 1, capacidad = 4 });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClaveDeServicioValida_PermiteAccesoDesdeBackend()
    {
        using var factory = new ReservasApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ServiceApiKeyAuthenticationHandler.HeaderName,
            factory.ServiceKey);

        var response = await client.GetAsync("/api/Mesas");

        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK,
            $"Respuesta: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task ReservaDuplicada_Devuelve409SinExponerErrorSql()
    {
        using var factory = new ReservasApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ServiceApiKeyAuthenticationHandler.HeaderName,
            factory.ServiceKey);

        var mesaResponse = await client.PostAsJsonAsync(
            "/api/Mesas",
            new { numero = 1, capacidad = 4 });
        Assert.Equal(System.Net.HttpStatusCode.Created, mesaResponse.StatusCode);

        var payload = new
        {
            nombreCliente = "Cliente HTTP",
            telefono = "70000000",
            fechaReserva = DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
            horaReserva = new TimeOnly(18, 0),
            cantidadPersonas = 2,
            mesaId = 1,
            observaciones = (string?)null
        };

        var primera = await client.PostAsJsonAsync("/api/Reservas", payload);
        var duplicada = await client.PostAsJsonAsync("/api/Reservas", payload);
        var cuerpo = await duplicada.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, duplicada.StatusCode);
        Assert.DoesNotContain("SqlException", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reserva", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClaveDeServicioNoConfigurada_CierraElAcceso()
    {
        using var factory = new ReservasApiFactory(serviceKeyConfigured: false);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ServiceApiKeyAuthenticationHandler.HeaderName,
            factory.ServiceKey);

        var response = await client.GetAsync("/api/Reservas");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UsuarioAutenticadoSinPermisoAdministrativo_Devuelve403()
    {
        using var factory = new ReservasApiFactory(authenticatedWithoutAdminScope: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Reservas");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed class ReservasApiFactory : WebApplicationFactory<Program>
    {
        private readonly bool _authenticatedWithoutAdminScope;
        private readonly bool _serviceKeyConfigured;
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly string? _previousConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        public string ServiceKey { get; } =
            Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        public ReservasApiFactory(
            bool authenticatedWithoutAdminScope = false,
            bool serviceKeyConfigured = true)
        {
            _authenticatedWithoutAdminScope = authenticatedWithoutAdminScope;
            _serviceKeyConfigured = serviceKeyConfigured;
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                "Server=localhost;Database=ReservasApiTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:ServiceApiKey"] =
                        _serviceKeyConfigured ? ServiceKey : null,
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=localhost;Database=ReservasApiTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;"
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<EasyMenuReservasDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<EasyMenuReservasDbContext>>();
                services.RemoveAll<EasyMenuReservasDbContext>();
                services.AddDbContext<EasyMenuReservasDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));

                if (_authenticatedWithoutAdminScope)
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestWithoutAdmin";
                        options.DefaultChallengeScheme = "TestWithoutAdmin";
                        options.DefaultForbidScheme = "TestWithoutAdmin";
                    }).AddScheme<AuthenticationSchemeOptions, TestWithoutAdminHandler>(
                        "TestWithoutAdmin",
                        _ => { });
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                _previousConnectionString);
        }
    }

    private sealed class TestWithoutAdminHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestWithoutAdminHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [new Claim("scope", "reservas.read")],
                Scheme.Name);
            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
