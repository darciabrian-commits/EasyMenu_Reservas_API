using EasyMenu.Reservas.API.Data;
using EasyMenu.Reservas.API.Services;
using EasyMenu.Reservas.API.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
 
var builder = WebApplication.CreateBuilder(args);
 
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services
    .AddAuthentication(ServiceApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ServiceApiKeyAuthenticationHandler>(
        ServiceApiKeyAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReservasAdministracion", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("scope", "reservas.admin"));
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("scope", "reservas.admin")
        .Build();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings__DefaultConnection para iniciar la API.");
}
 
builder.Services.AddDbContext<EasyMenuReservasDbContext>(options =>
    options.UseSqlServer(connectionString));
 
builder.Services.AddScoped<MesaService>();
builder.Services.AddScoped<ReservaService>();
 
var app = builder.Build();
 
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
 
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
 
app.Run();

public partial class Program
{
}
