using EasyMenu.Reservas.API.Data;
using EasyMenu.Reservas.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyMenu.Reservas.API.Services
{
    public class MesaService
    {
        private readonly EasyMenuReservasDbContext _context;

        public MesaService(EasyMenuReservasDbContext context)
        {
            _context = context;
        }

        public async Task<List<Mesa>> ObtenerTodasAsync()
        {
            return await _context.Mesas
                .AsNoTracking()
                .OrderBy(m => m.Numero)
                .ToListAsync();
        }

        public async Task<Mesa?> ObtenerPorIdAsync(int id)
        {
            return await _context.Mesas
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Mesa> CrearAsync(int numero, int capacidad)
        {
            if (numero <= 0)
                throw new ArgumentException("El número de mesa debe ser mayor que 0.");

            if (capacidad <= 0)
                throw new ArgumentException("La capacidad debe ser mayor que 0.");

            bool existeNumero = await _context.Mesas
                .AnyAsync(m => m.Numero == numero);

            if (existeNumero)
                throw new InvalidOperationException(
                    $"Ya existe una mesa con el número {numero}.");

            var mesa = new Mesa
            {
                Numero = numero,
                Capacidad = capacidad,
                Activa = true,
                FechaCreacion = DateTime.Now
            };

            _context.Mesas.Add(mesa);
            await _context.SaveChangesAsync();

            return mesa;
        }

        public async Task<Mesa?> ActualizarAsync(
            int id,
            int numero,
            int capacidad,
            bool activa)
        {
            var mesa = await _context.Mesas.FindAsync(id);

            if (mesa == null)
                return null;

            if (numero <= 0)
                throw new ArgumentException("El número de mesa debe ser mayor que 0.");

            if (capacidad <= 0)
                throw new ArgumentException("La capacidad debe ser mayor que 0.");

            bool numeroOcupado = await _context.Mesas
                .AnyAsync(m => m.Numero == numero && m.Id != id);

            if (numeroOcupado)
                throw new InvalidOperationException(
                    $"Ya existe otra mesa con el número {numero}.");

            mesa.Numero = numero;
            mesa.Capacidad = capacidad;
            mesa.Activa = activa;

            await _context.SaveChangesAsync();
            return mesa;
        }
    }
}

