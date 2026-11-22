import { distanceKm, type Coordinates } from '../lib/geo';
import type { CafeDetail } from '../types/cafe';
import { storeCafeDetails } from './osmCache';

/** Photon (komoot) sirve datos de OpenStreetMap y responde en milisegundos. */
const ENDPOINT = 'https://photon.komoot.io/api/';

/** Dos términos de nombre: cubren "Café…" y "…Coffee" (Starbucks, Syra…). */
const SEARCH_TERMS = ['cafe', 'coffee'];

const RADIUS_KM = 5;
const LIMIT_PER_TERM = 50;
const MAX_RESULTS = 60;
const REQUEST_TIMEOUT_MS = 8_000;

type PhotonFeature = {
  properties?: {
    name?: string;
    osm_key?: string;
    osm_value?: string;
    osm_type?: string;
    osm_id?: string | number;
    street?: string;
    housenumber?: string;
    postcode?: string;
    district?: string;
    locality?: string;
  };
  geometry?: { coordinates?: [number, number] } | null;
};

async function queryTerm(
  term: string,
  location: Coordinates,
): Promise<PhotonFeature[]> {
  const params = new URLSearchParams({
    q: term,
    lat: String(location.latitude),
    lon: String(location.longitude),
    limit: String(LIMIT_PER_TERM),
    osm_tag: 'amenity:cafe',
  });

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  try {
    const response = await fetch(`${ENDPOINT}?${params.toString()}`, {
      signal: controller.signal,
    });

    if (!response.ok) {
      throw new Error(`OpenStreetMap (Photon) respondió ${response.status}`);
    }

    const payload = (await response.json()) as { features?: PhotonFeature[] };
    return payload.features ?? [];
  } finally {
    clearTimeout(timer);
  }
}

function normalizeOsmType(value: string): 'node' | 'way' | 'relation' | null {
  switch (value.toLowerCase()) {
    case 'n':
    case 'node':
      return 'node';
    case 'w':
    case 'way':
      return 'way';
    case 'r':
    case 'relation':
      return 'relation';
    default:
      return null;
  }
}

function toCafe(feature: PhotonFeature): CafeDetail | null {
  const properties = feature.properties;
  const name = properties?.name?.trim();
  const coordinates = feature.geometry?.coordinates;
  const osmType =
    typeof properties?.osm_type === 'string'
      ? normalizeOsmType(properties.osm_type)
      : null;

  if (
    !name ||
    !properties ||
    properties.osm_key !== 'amenity' ||
    properties.osm_value !== 'cafe' ||
    !osmType ||
    properties.osm_id === undefined ||
    !coordinates ||
    typeof coordinates[0] !== 'number' ||
    typeof coordinates[1] !== 'number'
  ) {
    return null;
  }

  const street = [properties.street, properties.housenumber]
    .filter(Boolean)
    .join(' ');
  const address =
    [street, properties.postcode].filter(Boolean).join(', ') ||
    'Sin dirección registrada';
  const neighborhood = properties.district ?? properties.locality ?? '';

  return {
    id: `osm-${osmType}/${properties.osm_id}`,
    name,
    address,
    neighborhood,
    phone: '',
    hours: '',
    // GeoJSON guarda [longitud, latitud].
    latitude: coordinates[1],
    longitude: coordinates[0],
    rating: 0,
    reviewCount: 0,
    menu: [],
    reviews: [],
  };
}

/**
 * Cafés reales de OpenStreetMap alrededor de una posición (radio de 5 km),
 * ordenados por cercanía. Combina las dos búsquedas por nombre y descarta
 * lo que quede fuera del radio, porque Photon sesga por proximidad pero no
 * filtra por distancia.
 */
export async function queryPhotonCafes(
  location: Coordinates,
): Promise<CafeDetail[]> {
  const results = await Promise.all(
    SEARCH_TERMS.map((term) => queryTerm(term, location)),
  );

  const cafes = new Map<string, CafeDetail>();

  for (const feature of results.flat()) {
    const cafe = toCafe(feature);
    if (!cafe || distanceKm(location, cafe) > RADIUS_KM) {
      continue;
    }
    cafes.set(cafe.id, cafe);
  }

  // El mismo local puede figurar como nodo y como vía en OpenStreetMap: se
  // descarta un gemelo con el mismo nombre a menos de 75 m para no duplicar.
  const byName = new Map<string, CafeDetail>();
  const nearest: CafeDetail[] = [];

  const sorted = [...cafes.values()].sort(
    (a, b) => distanceKm(location, a) - distanceKm(location, b),
  );

  for (const cafe of sorted) {
    const key = cafe.name
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '')
      .toLowerCase()
      .trim();
    const twin = byName.get(key);

    if (twin && distanceKm(twin, cafe) * 1000 <= 75) {
      continue;
    }

    byName.set(key, cafe);
    nearest.push(cafe);

    if (nearest.length >= MAX_RESULTS) {
      break;
    }
  }

  for (const cafe of nearest) {
    storeCafeDetails(cafe);
  }

  return nearest;
}
