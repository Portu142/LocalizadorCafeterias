import type { CafePhoto, CafeTag } from '../types/cafe';

type OsmTags = Record<string, string | undefined>;

const YES = new Set(['yes', 'only', 'designated']);

/**
 * Traduce las etiquetas de OpenStreetMap al vocabulario de la app. OSM no
 * tiene una etiqueta estándar para enchufes en cafeterías: se usan las que
 * más aparecen en la práctica (`power_supply`, `socket:*`).
 */
export function tagsFromOsm(tags: OsmTags): CafeTag[] {
  const result: CafeTag[] = [];
  const internet = tags.internet_access?.toLowerCase();

  if (internet && ['wlan', 'wifi', 'yes'].includes(internet)) {
    result.push('wifi');
  }
  if (YES.has(tags.outdoor_seating ?? '')) {
    result.push('terraza');
  }
  if (
    YES.has(tags.power_supply ?? '') ||
    Object.keys(tags).some((key) => key.startsWith('socket:') && tags[key] !== 'no')
  ) {
    result.push('enchufes');
  }
  if (tags.wheelchair === 'yes') {
    result.push('accesible');
  }
  if (YES.has(tags.takeaway ?? '')) {
    result.push('para-llevar');
  }
  if (YES.has(tags.dog ?? '')) {
    result.push('mascotas');
  }
  if (YES.has(tags['diet:vegan'] ?? '')) {
    result.push('vegano');
  }

  return result;
}

const COMMONS_FILE_PAGE = 'https://commons.wikimedia.org/wiki/';

/** Nombre de archivo de Commons ("File:Foo.jpg") a partir de un valor de OSM. */
function commonsFile(value: string | undefined): string | null {
  if (!value) {
    return null;
  }

  const trimmed = value.trim();

  // `wikimedia_commons=File:Foo.jpg` o `image=https://commons.wikimedia.org/wiki/File:Foo.jpg`
  const fromUrl = /^https:\/\/commons\.wikimedia\.org\/wiki\/(File:[^?#]+)$/i.exec(trimmed);
  const raw = fromUrl ? decodeURIComponent(fromUrl[1]) : trimmed;

  return /^File:.+\.(jpe?g|png|webp)$/i.test(raw) ? raw.replace(/ /g, '_') : null;
}

/**
 * Fotos de una cafetería de OpenStreetMap. Solo se aceptan imágenes de
 * Wikimedia Commons: tienen licencia libre, permiten enlazarlas y su dominio
 * está permitido en la CSP. Cualquier otra URL de `image=` se ignora.
 */
export function photosFromOsm(tags: OsmTags): CafePhoto[] {
  const files = new Set<string>();

  for (const value of [tags.wikimedia_commons, tags.image]) {
    const file = commonsFile(value);
    if (file) {
      files.add(file);
    }
  }

  return [...files].map((file) => ({
    id: file,
    // Special:FilePath redirige a la miniatura del tamaño pedido.
    url: `${COMMONS_FILE_PAGE}Special:FilePath/${encodeURIComponent(file.slice(5))}?width=960`,
    author: 'Wikimedia Commons',
    license: 'Ver licencia',
    sourceUrl: `${COMMONS_FILE_PAGE}File:${encodeURIComponent(file.slice(5))}`,
  }));
}

/** Solo enlaces web normales: descarta `javascript:` y similares. */
function safeUrl(value: string | undefined): string | undefined {
  if (!value) {
    return undefined;
  }

  const candidate = /^https?:\/\//i.test(value.trim()) ? value.trim() : `https://${value.trim()}`;

  try {
    const url = new URL(candidate);
    return url.protocol === 'https:' || url.protocol === 'http:' ? url.href : undefined;
  } catch {
    return undefined;
  }
}

/** Datos de la ficha que salen de las etiquetas de un elemento de OSM. */
export function detailsFromOsmTags(tags: OsmTags) {
  return {
    phone: tags.phone ?? tags['contact:phone'] ?? '',
    hours: tags.opening_hours ?? '',
    tags: tagsFromOsm(tags),
    photos: photosFromOsm(tags),
    website: safeUrl(tags.website ?? tags['contact:website']),
    // OSM usa varias claves para la carta; se aceptan las más habituales.
    menuUrl: safeUrl(tags['website:menu'] ?? tags['menu:url'] ?? tags['contact:menu']),
  };
}
