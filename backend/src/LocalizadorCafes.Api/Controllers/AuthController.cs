using System.ComponentModel.DataAnnotations;
using LocalizadorCafes.Api.Auth;
using LocalizadorCafes.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocalizadorCafes.Api.Controllers;

/// <summary>
/// Registro e inicio de sesión. La sesión viaja en una cookie HttpOnly
/// (inaccesible desde JavaScript), así que el frontend nunca maneja tokens.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly AdminAccess _adminAccess;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        AdminAccess adminAccess)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _adminAccess = adminAccess;
    }

    public record RegisterRequest(
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        [MaxLength(254, ErrorMessage = "El email es demasiado largo.")]
        string Email,
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MaxLength(128, ErrorMessage = "La contraseña es demasiado larga.")]
        string Password,
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres.")]
        [MaxLength(40, ErrorMessage = "El nombre no puede superar los 40 caracteres.")]
        string DisplayName);

    public record LoginRequest(
        [Required(ErrorMessage = "El email es obligatorio.")] string Email,
        [Required(ErrorMessage = "La contraseña es obligatoria.")] string Password);

    public record UserDto(Guid Id, string Email, string DisplayName, bool IsAdmin);

    /// <summary>Crea una cuenta e inicia sesión con ella.</summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["account"] = result.Errors.Select(error => error.Description).Distinct().ToArray()
                })
            {
                Title = "No se pudo crear la cuenta",
                Detail = string.Join(" ", result.Errors.Select(error => error.Description).Distinct()),
                Status = StatusCodes.Status400BadRequest
            });
        }

        // El rol va antes de iniciar sesión para que la cookie ya lo incluya.
        await _adminAccess.EnsureRoleAsync(user, _userManager, _roleManager);
        await _signInManager.SignInAsync(user, isPersistent: true);
        return Ok(await ToDtoAsync(user));
    }

    /// <summary>Inicia sesión con email y contraseña.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim();
        var result = await _signInManager.PasswordSignInAsync(
            email,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            // Bloqueo indefinido = suspendida por un administrador.
            var locked = await _userManager.FindByNameAsync(email);
            var suspended = locked?.LockoutEnd > DateTimeOffset.UtcNow.AddYears(10);

            return Problem(
                title: suspended ? "Cuenta suspendida" : "Cuenta bloqueada temporalmente",
                detail: suspended
                    ? "Esta cuenta ha sido suspendida por un administrador."
                    : "Demasiados intentos fallidos. Vuelve a intentarlo en unos minutos.",
                statusCode: suspended ? StatusCodes.Status403Forbidden : StatusCodes.Status429TooManyRequests);
        }

        if (!result.Succeeded)
        {
            // Mismo mensaje para email inexistente y contraseña errónea: no se
            // revela qué cuentas existen.
            return Problem(
                title: "No se pudo iniciar sesión",
                detail: "El email o la contraseña no son correctos.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await _userManager.FindByNameAsync(email);
        if (user is null)
        {
            return Unauthorized();
        }

        // Si el email se acaba de añadir a Admin:Emails, se da el rol y se
        // renueva la cookie para que lo incluya.
        if (await _adminAccess.EnsureRoleAsync(user, _userManager, _roleManager))
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        return Ok(await ToDtoAsync(user));
    }

    /// <summary>Cierra la sesión actual.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    /// <summary>
    /// Usuario con sesión iniciada, o 204 sin contenido si no hay sesión. No es
    /// un error: la web lo consulta en cada visita, también sin cuenta.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<UserDto>> Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return NoContent();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null || await _userManager.IsLockedOutAsync(user))
        {
            // Cuenta borrada o suspendida: se cierra la sesión que quedara abierta.
            await _signInManager.SignOutAsync();
            return NoContent();
        }

        return Ok(await ToDtoAsync(user));
    }

    private async Task<UserDto> ToDtoAsync(AppUser user) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            await _userManager.IsInRoleAsync(user, AdminAccess.Role));
}
