namespace LocalizadorCafes.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            headers["Cross-Origin-Resource-Policy"] = "cross-origin";
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }
        else if (context.Request.Path.StartsWithSegments("/admin"))
        {
            // Panel de administración (HTML): solo recursos propios, sin
            // JavaScript en línea, sin incrustarse en otras webs y sin caché
            // (son datos privados que no deben quedar en el navegador).
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "same-origin";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            headers["Cache-Control"] = "no-store";
            headers["Content-Security-Policy"] =
                "default-src 'self'; script-src 'self'; style-src 'self'; " +
                "img-src 'self' https://thumb.wikimedia.org https://upload.wikimedia.org https://commons.wikimedia.org; " +
                "form-action 'self'; frame-ancestors 'none'; base-uri 'self'; object-src 'none'";
        }

        await _next(context);
    }
}
