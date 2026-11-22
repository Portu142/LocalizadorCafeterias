import type { Coordinates } from '../lib/geo';
import type { CafeDetail } from '../types/cafe';
import { getCachedCafe, isCompleteInCache, storeCafeDetails } from './osmCache';
import { detailsFromOsmTags } from './osmTags';

/** Endpoints públicos de Overpass API (OpenStreetMap), con respaldo. */
const ENDPOINTS = [
  'https://overpass-api.de/api/interpreter',
  'https://overpass.kumi.systems/api/interpreter',
  'https://overpass.private.coffee/api/interpreter',
];

/** Espera máxima por endpoint antes de pasar al siguiente respaldo. */
const REQUEST_TIMEOUT_MS = 15_000;

/** Espera máxima para completar con Overpass una ficha que ya llegó de Photon. */
const ENRICH_TIMEOUT_MS = 4_000;

const RADIUS_KM = 5;
const MAX_RESULTS = 60;

type OverpassElement = {
  type: 'node' | 'way' | 'relation';
  id: number;
  lat?: number;
  lon?: number;
  center?: { lat: number; lon: number };
  tags?: Record<string, string>;
};

function buildAroundQuery(latitude: number, longitude: number): string {
  const radiusM = RADIUS_KM * 1000;

  // Solo nodos y vías: las relaciones de café son muy raras y encarecen mucho
  // la consulta (las instancias públicas devuelven 504 con ellas).
  return (
    `[out:json][timeout:20];(` +
    `node["amenity"="cafe"](around:${radiusM},${latitude},${longitude});` +
    `way["amenity"="cafe"](around:${radiusM},${latitude},${longitude});` +
    `);out center;`
  );
}

function buildByIdQuery(type: 'node' | 'way' | 'relation', id: string): string {
  return `[out:json][timeout:25];${type}(${id});out center;`;
}

type QueryOptions = {
  timeoutMs?: number;
  /** Cuántos endpoints de respaldo probar (por defecto, todos). */
  maxEndpoints?: number;
};

async function queryOverpass(
  query: string,
  { timeoutMs = REQUEST_TIMEOUT_MS, maxEndpoints = ENDPOINTS.length }: QueryOptions = {},
): Promise<OverpassElement[]> {
  let lastError: unknown = null;

  for (const endpoint of ENDPOINTS.slice(0, maxEndpoints)) {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), timeoutMs);

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `data=${encodeURIComponent(query)}`,
        signal: controller.signal,
      });

      if (!response.ok) {
        throw new Error(`OpenStreetMap respondió ${response.status}`);
      }

      const payload = (await response.json()) as { elements?: OverpassElement[] };
      return payload.elements ?? [];
    } catch (error) {
      // Un timeout no debe confundirse con un error de red: se reintenta abajo.
      lastError =
        error instanceof Error && error.name === 'AbortError'
          ? new Error('OpenStreetMap tardó demasiado en responder.')
          : error;
    } finally {
      clearTimeout(timer);
    }
  }

  throw lastError instanceof Error
    ? lastError
    : new Error('No se pudo consultar OpenStreetMap.');
}

function coordinatesOf(element: OverpassElement): Coordinates | null {
  const latitude = element.lat ?? element.center?.lat;
  const longitude = element.lon ?? element.center?.lon;

  if (typeof latitude !== 'number' || typeof longitude !== 'number') {
    return null;
  }

  return { latitude, longitude };
}

function toCafeDetail(element: OverpassElement): CafeDetail | null {
  const tags = element.tags ?? {};
  const name = tags.name?.trim();
  const coordinates = coordinatesOf(element);

  if (!name || !coordinates) {
    return null;
  }

  const street = [tags['addr:street'], tags['addr:housenumber']]
    .filter(Boolean)
    .join(' ');
  const address =
    [street, tags['addr:postcode']].filter(Boolean).join(', ') ||
    'Sin dirección registrada';

  const neighborhood =
    tags['addr:suburb'] ??
    tags['addr:neighbourhood'] ??
    tags['addr:quarter'] ??
    tags['addr:district'] ??
    '';

  return {
    id: `osm-${element.type}/${element.id}`,
    name,
    address,
    neighborhood,
    ...detailsFromOsmTags(tags),
    latitude: coordinates.latitude,
    longitude: coordinates.longitude,
    rating: 0,
    reviewCount: 0,
    menu: [],
    reviews: [],
  };
}

function distanceTo(from: Coordinates, cafe: CafeDetail): number {
  const dLat = cafe.latitude - from.latitude;
  const dLng = (cafe.longitude - from.longitude) * Math.cos((from.latitude * Math.PI) / 180);
  return dLat * dLat + dLng * dLng;
}

/**
 * Cafés reales de OpenStreetMap alrededor de una posición (radio de 5 km),
 * ordenados por cercanía. Lanza un error si ningún endpoint responde.
 */
export async function queryOverpassNearby(
  location: Coordinates,
): Promise<CafeDetail[]> {
  const elements = await queryOverpass(
    buildAroundQuery(location.latitude, location.longitude),
  );

  const cafes = elements
    .map(toCafeDetail)
    .filter((cafe): cafe is CafeDetail => cafe !== null)
    .sort((a, b) => distanceTo(location, a) - distanceTo(location, b))
    .slice(0, MAX_RESULTS);

  for (const cafe of cafes) {
    storeCafeDetails(cafe, true);
  }

  return cafes;
}

const OSM_API = 'https://api.openstreetmap.org/api/0.6';

/** Etiquetas de un único elemento desde la API oficial de OSM (uso ligero:
 *  una petición al abrir la ficha). Devuelve `null` si falla o tarda. */
async function fetchElementTags(
  type: 'node' | 'way' | 'relation',
  id: string,
): Promise<Record<string, string> | null> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), ENRICH_TIMEOUT_MS);

  try {
    const response = await fetch(`${OSM_API}/${type}/${id}.json`, {
      signal: controller.signal,
    });
    if (!response.ok) {
      return null;
    }

    const payload = (await response.json()) as {
      elements?: { tags?: Record<string, string> }[];
    };
    return payload.elements?.[0]?.tags ?? null;
  } catch {
    return null;
  } finally {
    clearTimeout(timer);
  }
}

function mergeOsmTags(cafe: CafeDetail, tags: Record<string, string>): CafeDetail {
  const details = detailsFromOsmTags(tags);

  return {
    ...cafe,
    ...details,
    phone: cafe.phone || details.phone,
    hours: cafe.hours || details.hours,
  };
}

/** Tipo e id numérico de un id `osm-node/123`; null si no es un nodo o vía. */
function parseNodeOrWayId(id: string): { type: 'node' | 'way'; ref: string } | null {
  const [prefix, ref] = id.split('/');
  const type = prefix === 'osm-node' ? 'node' : prefix === 'osm-way' ? 'way' : null;

  return type && ref && /^\d+$/.test(ref) ? { type, ref } : null;
}

/**
 * Completa con sus etiquetas de OSM (servicios, horario, web…) una lista de
 * cafés que llegó de Photon. Usa la descarga múltiple de la API oficial: una
 * petición por tipo de elemento para toda la lista. Si falla, devuelve la
 * lista tal cual y cada ficha se completará al abrirla.
 */
export async function enrichWithOsmTags(cafes: CafeDetail[]): Promise<CafeDetail[]> {
  const idsByType = new Map<'node' | 'way', string[]>();

  for (const cafe of cafes) {
    const parsed = isCompleteInCache(cafe.id) ? null : parseNodeOrWayId(cafe.id);
    if (parsed) {
      idsByType.set(parsed.type, [...(idsByType.get(parsed.type) ?? []), parsed.ref]);
    }
  }

  const tagsById = new Map<string, Record<string, string>>();

  await Promise.all(
    [...idsByType].map(async ([type, ids]) => {
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), ENRICH_TIMEOUT_MS);

      try {
        const response = await fetch(`${OSM_API}/${type}s.json?${type}s=${ids.join(',')}`, {
          signal: controller.signal,
        });
        if (!response.ok) {
          return;
        }

        const payload = (await response.json()) as {
          elements?: { type: string; id: number; tags?: Record<string, string> }[];
        };
        for (const element of payload.elements ?? []) {
          tagsById.set(`osm-${element.type}/${element.id}`, element.tags ?? {});
        }
      } catch {
        // Sin datos extra: la lista se muestra igual.
      } finally {
        clearTimeout(timer);
      }
    }),
  );

  return cafes.map((cafe) => {
    const tags = tagsById.get(cafe.id);
    if (!tags) {
      return getCachedCafe(cafe.id) ?? cafe;
    }

    const enriched = mergeOsmTags(cafe, tags);
    storeCafeDetails(enriched, true);
    return enriched;
  });
}

/** Detalle de un café de OpenStreetMap por id (`osm-node/123`), desde caché o Overpass. */
export async function queryCafeById(id: string): Promise<CafeDetail | null> {
  const cached = getCachedCafe(id);
  if (cached && isCompleteInCache(id)) {
    return cached;
  }

  const match = /^osm-([a-z]+)\/(\d+)$/i.exec(id);
  if (!match) {
    return null;
  }

  const type = match[1].toLowerCase();
  const normalizedType =
    type === 'n' || type === 'node'
      ? 'node'
      : type === 'w' || type === 'way'
        ? 'way'
        : type === 'r' || type === 'relation'
          ? 'relation'
          : null;

  if (!normalizedType) {
    return null;
  }

  // Ficha de Photon (sin etiquetas): se completa con la API oficial de OSM,
  // que devuelve un solo elemento en milisegundos y no depende de Overpass.
  if (cached) {
    const tags = await fetchElementTags(normalizedType, match[2]);
    if (tags) {
      const enriched = mergeOsmTags(cached, tags);
      storeCafeDetails(enriched, true);
      return enriched;
    }
  }

  // Si ya tenemos la ficha de Photon, completar servicios y fotos es un
  // extra: se limita a unos segundos y, si falla, se muestra lo que hay.
  let elements: OverpassElement[];
  try {
    elements = await queryOverpass(
      buildByIdQuery(normalizedType, match[2]),
      cached ? { timeoutMs: ENRICH_TIMEOUT_MS, maxEndpoints: 1 } : {},
    );
  } catch (error) {
    if (cached) {
      return cached;
    }
    throw error;
  }

  const cafe = elements.map(toCafeDetail).find((item): item is CafeDetail => item !== null);

  if (cafe) {
    storeCafeDetails(cafe, true);
    return cafe;
  }

  return cached ?? null;
}
