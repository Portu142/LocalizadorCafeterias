import { writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig, loadEnv, type ProxyOptions } from 'vite';

const DEFAULT_SITE_URL = 'https://example.github.io/localizador-cafes/';

/** Hosts que el frontend consulta directamente en el modo demo (sin API). */
const OSM_CONNECT_SRC = [
  'https://api.openstreetmap.org',
  'https://photon.komoot.io',
  'https://overpass-api.de',
  'https://overpass.kumi.systems',
  'https://overpass.private.coffee',
];

function contentSecurityPolicy(apiOrigin: string | null) {
  // La API local va por HTTP: si se configura VITE_API_URL hay que permitir su
  // origen en connect-src o el navegador bloquea todas las peticiones.
  const connectSrc = ["'self'", ...OSM_CONNECT_SRC];
  if (apiOrigin) {
    connectSrc.push(apiOrigin);
  }

  return [
    "default-src 'self'",
    "base-uri 'self'",
    "form-action 'self'",
    "object-src 'none'",
    // Fotos: solo Wikimedia Commons (Special:FilePath redirige a sus miniaturas).
    // Fotos de la comunidad: las sirve la API (/api/photos/…).
    `img-src 'self' data: blob: https://tile.openstreetmap.org https://commons.wikimedia.org https://upload.wikimedia.org https://thumb.wikimedia.org${apiOrigin ? ` ${apiOrigin}` : ''}`,
    "script-src 'self'",
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
    "font-src 'self' data: https://fonts.gstatic.com",
    `connect-src ${connectSrc.join(' ')}`,
    "manifest-src 'self'",
  ].join('; ');
}

/**
 * Proxy de `vite preview` hacia la API para el build del túnel. Cloudflare
 * indica en `Cf-Visitor` si el visitante usa HTTPS y en `CF-Connecting-IP` su
 * dirección: se reenvían como X-Forwarded-Proto/For para que la API marque la
 * cookie de sesión como Secure y aplique los límites por visitante. La API solo
 * acepta esas cabeceras cuando la petición llega desde la propia máquina.
 */
const apiProxy: ProxyOptions = {
  target: 'http://localhost:5067',
  changeOrigin: true,
  configure: (proxy) => {
    proxy.on('proxyReq', (proxyReq, request) => {
      const visitor = request.headers['cf-visitor'];
      const clientIp = request.headers['cf-connecting-ip'];

      if (typeof visitor === 'string' && visitor.includes('"https"')) {
        proxyReq.setHeader('X-Forwarded-Proto', 'https');
      }
      if (typeof clientIp === 'string') {
        proxyReq.setHeader('X-Forwarded-For', clientIp);
      }
    });
  },
};

function apiOriginOf(value: string | undefined): string | null {
  if (!value?.trim()) {
    return null;
  }

  try {
    return new URL(value.trim()).origin;
  } catch {
    return null;
  }
}

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const siteUrl = `${(env.VITE_SITE_URL?.trim() || DEFAULT_SITE_URL).replace(/\/+$/, '')}/`;
  const ogImageUrl = new URL('og-image.png', siteUrl).href;
  const contentSecurityPolicyHeader = contentSecurityPolicy(
    apiOriginOf(env.VITE_API_URL),
  );

  return {
    base: './',
    // Permite compartir el build de producción con un túnel de Cloudflare
    // (`cloudflared tunnel --url http://localhost:4173`) sin que Vite bloquee
    // el Host público con un 403. El proxy sirve la API en el mismo origen
    // cuando se compila con VITE_API_URL=same-origin.
    preview: {
      allowedHosts: ['.trycloudflare.com'],
      // Mismas cabeceras de seguridad que nginx (Docker) para el build que se
      // comparte por el túnel. La CSP va en un <meta> del propio index.html.
      headers: {
        'X-Content-Type-Options': 'nosniff',
        'X-Frame-Options': 'DENY',
        'Referrer-Policy': 'strict-origin-when-cross-origin',
        'Permissions-Policy': 'geolocation=(self), microphone=(), camera=(), payment=()',
        'Cross-Origin-Opener-Policy': 'same-origin',
      },
      proxy: {
        '/api': apiProxy,
        '/health': apiProxy,
        // Panel de administración del backend (páginas HTML en /admin).
        '/admin': apiProxy,
      },
    },
    plugins: [
      react(),
      {
        name: 'site-meta',
        transformIndexHtml(html) {
          return html
            .replaceAll('%SITE_URL%', siteUrl)
            .replaceAll('%OG_IMAGE_URL%', ogImageUrl);
        },
      },
      {
        name: 'security-meta',
        apply: 'build',
        enforce: 'post',
        transformIndexHtml(html) {
          // Una CSP en <meta> solo protege lo que aparece DESPUÉS de ella: se
          // inserta justo tras el charset, antes de hojas de estilo y scripts.
          return html.replace(
            /(<meta charset="UTF-8" \/>)/i,
            `$1\n    <meta http-equiv="Content-Security-Policy" content="${contentSecurityPolicyHeader}" />`,
          );
        },
      },
      {
        name: 'sitemap',
        apply: 'build',
        closeBundle() {
          const dist = resolve(process.cwd(), 'dist');
          const lastmod = new Date().toISOString().slice(0, 10);

          writeFileSync(
            resolve(dist, 'sitemap.xml'),
            `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n  <url>\n    <loc>${siteUrl}</loc>\n    <lastmod>${lastmod}</lastmod>\n    <changefreq>weekly</changefreq>\n    <priority>1.0</priority>\n  </url>\n</urlset>\n`,
            'utf8',
          );

          writeFileSync(
            resolve(dist, 'robots.txt'),
            // El panel de administración y la API no deben indexarse.
            `User-agent: *\nAllow: /\nDisallow: /admin\nDisallow: /api/\n\nSitemap: ${siteUrl}sitemap.xml\n`,
            'utf8',
          );
        },
      },
    ],
  };
});
