using EasyMenu.Reservas.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyMenu.Reservas.API.Data
{
    public class EasyMenuReservasDbContext : DbContext
    {
        public EasyMenuReservasDbContext(
            DbContextOptions<EasyMenuReservasDbContext> options)
            : base(options)
        {
        }

        public DbSet<Mesa> Mesas { get; set; }

        public DbSet<Reserva> Reservas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Mesa>()
                .HasIndex(m => m.Numero)
                .IsUnique();

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Mesa)
                .WithMany(m => m.Reservas)
                .HasForeignKey(r => r.MesaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .Property(r => r.Estado)
                .HasMaxLength(20);

            modelBuilder.Entity<Reserva>()
                .HasIndex(r => new
                {
                    r.MesaId,
                    r.FechaReserva,
                    r.HoraReserva
                })
                .IsUnique()
                .HasFilter("[Estado] <> N'CANCELADA'");
        }
    }
}