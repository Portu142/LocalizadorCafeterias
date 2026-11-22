using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LocalizadorCafes.Infrastructure.Identity;

/// <summary>
/// Cuentas en memoria para el modo demo sin PostgreSQL. Solo contiene las
/// tablas de Identity: las cafeterías siguen en <c>InMemoryCafeRepository</c>.
/// Las cuentas se pierden al reiniciar, igual que las reseñas del modo demo.
/// </summary>
public class InMemoryIdentityDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public InMemoryIdentityDbContext(DbContextOptions<InMemoryIdentityDbContext> options)
        : base(options)
    {
    }
}
