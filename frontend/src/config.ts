const configuredApiUrl =
  (import.meta.env.VITE_API_URL as string | undefined)
    ?.trim()
    .replace(/\/$/, '') ?? '';

/**
 * URL de la API. `same-origin` sirve la API mediante el proxy del preview
 * (útil para compartir el build por un túnel) y deja las rutas relativas.
 */
export const API_URL = configuredApiUrl === 'same-origin' ? '' : configuredApiUrl;

export const USE_API = Boolean(configuredApiUrl);

export const IS_DEMO_MODE = !USE_API;
