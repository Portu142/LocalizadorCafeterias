using System.ComponentModel.DataAnnotations;
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

    public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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

    public record UserDto(Guid Id, string Email, string DisplayName);

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

        await _signInManager.SignInAsync(user, isPersistent: true);
        return Ok(ToDto(user));
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
            return Problem(
                title: "Cuenta bloqueada temporalmente",
                detail: "Demasiados intentos fallidos. Vuelve a intentarlo en unos minutos.",
                statusCode: StatusCodes.Status429TooManyRequests);
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
        return user is null ? Unauthorized() : Ok(ToDto(user));
    }

    /// <summary>Cierra la sesión actual.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    /// <summary>Usuario con sesión iniciada, o 401 si no hay sesión.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(ToDto(user));
    }

    private static UserDto ToDto(AppUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName);
}
