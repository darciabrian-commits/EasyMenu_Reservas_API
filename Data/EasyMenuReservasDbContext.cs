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
        }
    }
}