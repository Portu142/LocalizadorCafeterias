import type { Coordinates } from '../lib/geo';
import type { CafeDetail } from '../types/cafe';
import { getCachedCafe, storeCafeDetails } from './osmCache';

/** Endpoints públicos de Overpass API (OpenStreetMap), con respaldo. */
const ENDPOINTS = [
  'https://overpass-api.de/api/interpreter',
  'https://overpass.kumi.systems/api/interpreter',
  'https://overpass.private.coffee/api/interpreter',
];

/** Espera máxima por endpoint antes de pasar al siguiente respaldo. */
const REQUEST_TIMEOUT_MS = 15_000;

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

async function queryOverpass(query: string): Promise<OverpassElement[]> {
  let lastError: unknown = null;

  for (const endpoint of ENDPOINTS) {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

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
    phone: tags.phone ?? tags['contact:phone'] ?? '',
    hours: tags.opening_hours ?? '',
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
    storeCafeDetails(cafe);
  }

  return cafes;
}

/** Detalle de un café de OpenStreetMap por id (`osm-node/123`), desde caché o Overpass. */
export async function queryCafeById(id: string): Promise<CafeDetail | null> {
  const cached = getCachedCafe(id);
  if (cached) {
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

  const elements = await queryOverpass(buildByIdQuery(normalizedType, match[2]));
  const cafe = elements.map(toCafeDetail).find((item): item is CafeDetail => item !== null);

  if (cafe) {
    storeCafeDetails(cafe);
  }

  return cafe ?? null;
}
