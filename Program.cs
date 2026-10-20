using EasyMenu.Reservas.API.Data;
using EasyMenu.Reservas.API.Services;
using Microsoft.EntityFrameworkCore;
 
var builder = WebApplication.CreateBuilder(args);
 
builder.Services.AddOpenApi();
builder.Services.AddControllers();
 
builder.Services.AddDbContext<EasyMenuReservasDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));
 
builder.Services.AddScoped<MesaService>();
builder.Services.AddScoped<ReservaService>();
 
var app = builder.Build();
 
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
 
app.UseHttpsRedirection();
app.MapControllers();
 
app.Run();
