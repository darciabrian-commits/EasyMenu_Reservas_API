using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EasyMenu.Reservas.API.Security;

public sealed class ServiceApiKeyAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ReservasServiceKey";
    public const string HeaderName = "X-Reservas-Service-Key";
    private const string ConfigurationKey = "Authentication:ServiceApiKey";
    private const int MinimumKeyLengthBytes = 32;

    private readonly IConfiguration _configuration;

    public ServiceApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = _configuration[ConfigurationKey];
        if (string.IsNullOrEmpty(configuredKey) ||
            Encoding.UTF8.GetByteCount(configuredKey) < MinimumKeyLengthBytes)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.TryGetValue(HeaderName, out var suppliedValues) ||
            suppliedValues.Count != 1)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var expectedBytes = Encoding.UTF8.GetBytes(configuredKey);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedValues[0] ?? string.Empty);
        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes))
        {
            return Task.FromResult(AuthenticateResult.Fail("Credencial de servicio inválida."));
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "EasyMenuWebService"),
                new Claim("scope", "reservas.admin")
            ],
            SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
