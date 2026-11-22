using Microsoft.AspNetCore.Identity;

namespace LocalizadorCafes.Infrastructure.Identity;

/// <summary>Cuenta de usuario. El email es también el nombre de usuario.</summary>
public class AppUser : IdentityUser<Guid>
{
    /// <summary>Nombre público con el que se firman reseñas y fotos.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
