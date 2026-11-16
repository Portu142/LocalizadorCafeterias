import { writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig, loadEnv } from 'vite';

const DEFAULT_SITE_URL = 'https://example.github.io/localizador-cafes/';

function contentSecurityPolicy(apiOrigin: string | null) {
  // La API local va por HTTP: si se configura VITE_API_URL hay que permitir su
  // origen en connect-src o el navegador bloquea todas las peticiones.
  const connectSrc = ["'self'", 'https:'];
  if (apiOrigin) {
    connectSrc.push(apiOrigin);
  }

  return [
    "default-src 'self'",
    "base-uri 'self'",
    "form-action 'self'",
    "object-src 'none'",
    "img-src 'self' data: blob: https://*.tile.openstreetmap.org https://tile.openstreetmap.org",
    "script-src 'self'",
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
    "font-src 'self' data: https://fonts.gstatic.com",
    `connect-src ${connectSrc.join(' ')}`,
    "manifest-src 'self'",
  ].join('; ');
}

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
    // el Host público con un 403.
    preview: {
      allowedHosts: ['.trycloudflare.com'],
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
          return html.replace(
            '</head>',
            `    <meta http-equiv="Content-Security-Policy" content="${contentSecurityPolicyHeader}" />\n  </head>`,
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
            `User-agent: *\nAllow: /\n\nSitemap: ${siteUrl}sitemap.xml\n`,
            'utf8',
          );
        },
      },
    ],
  };
});
