import { API_URL, USE_API } from '../config';
import type { Coordinates } from '../lib/geo';
import { queryPhotonCafes } from './photon';
import { queryCafeById, queryOverpassNearby } from './overpass';
import type {
  CafeDetail,
  CafeSummary,
  CreateReviewRequest,
  Review,
} from '../types/cafe';

const MOCK_REVIEWS_KEY = 'localizador_cafes_mock_reviews';

function getStoredReviews(): Record<string, Review[]> {
  try {
    const raw = localStorage.getItem(MOCK_REVIEWS_KEY);
    return raw ? (JSON.parse(raw) as Record<string, Review[]>) : {};
  } catch {
    return {};
  }
}

function saveStoredReviews(reviews: Record<string, Review[]>) {
  try {
    localStorage.setItem(MOCK_REVIEWS_KEY, JSON.stringify(reviews));
  } catch {
    // Si el navegador bloquea localStorage, la reseña solo vive en memoria.
  }
}

function withStoredReviews(cafe: CafeDetail): CafeDetail {
  const stored = getStoredReviews()[cafe.id] ?? [];
  const reviews = [...cafe.reviews, ...stored].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );

  const rating = reviews.length
    ? Math.round(
        (reviews.reduce((sum, review) => sum + review.rating, 0) / reviews.length) * 10,
      ) / 10
    : 0;

  return {
    ...cafe,
    reviews,
    rating,
    reviewCount: reviews.length,
  };
}

function toSummary(cafe: CafeDetail): CafeSummary {
  return {
    id: cafe.id,
    name: cafe.name,
    address: cafe.address,
    neighborhood: cafe.neighborhood,
    rating: cafe.rating,
    reviewCount: cafe.reviewCount,
    latitude: cafe.latitude,
    longitude: cafe.longitude,
  };
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const headers = new Headers(options?.headers);
  if (!headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as
      | { detail?: string; title?: string }
      | null;

    throw new Error(
      problem?.detail ?? problem?.title ?? 'No se pudo conectar con la API.',
    );
  }

  return response.json() as Promise<T>;
}

export type CafeSource = 'api' | 'osm';

export type CafeSearchResult = {
  cafes: CafeSummary[];
  source: CafeSource;
};

async function queryOpenStreetMap(location: Coordinates): Promise<CafeDetail[]> {
  let cafes: CafeDetail[] = [];

  try {
    cafes = await queryPhotonCafes(location);
  } catch {
    // Photon no respondió: se intenta directamente con Overpass.
  }

  if (cafes.length === 0) {
    cafes = await queryOverpassNearby(location);
  }

  return cafes;
}

export async function fetchCafesWithSource(
  location?: Coordinates | null,
): Promise<CafeSearchResult> {
  if (USE_API) {
    const params = new URLSearchParams();

    if (location) {
      params.set('lat', String(location.latitude));
      params.set('lng', String(location.longitude));
      params.set('radiusKm', '5');
    }

    const query = params.toString();
    const cafes = await request<CafeSummary[]>(
      `/api/cafes${query ? `?${query}` : ''}`,
    );

    // La base de datos de la API solo cubre Madrid: si no hay cafeterías cerca
    // de la ubicación real, se completan con OpenStreetMap para que la app sea
    // útil en cualquier ciudad.
    if (cafes.length > 0 || !location) {
      return { cafes, source: 'api' };
    }

    try {
      const osmCafes = await queryOpenStreetMap(location);
      if (osmCafes.length > 0) {
        return {
          cafes: osmCafes.map(withStoredReviews).map(toSummary),
          source: 'osm',
        };
      }
    } catch {
      // OpenStreetMap tampoco respondió: se deja la lista vacía de la API.
    }

    return { cafes, source: 'api' };
  }

  // Modo demo sin backend: siempre se consultan cafeterías reales de
  // OpenStreetMap. Photon responde en milisegundos y, si no encuentra nada,
  // se completa con Overpass (que busca por etiqueta y no por nombre).
  if (location) {
    const cafes = await queryOpenStreetMap(location);
    return {
      cafes: cafes.map(withStoredReviews).map(toSummary),
      source: 'osm',
    };
  }

  return { cafes: [], source: 'osm' };
}

export async function fetchCafe(id: string): Promise<CafeDetail> {
  // Los cafés de OpenStreetMap (id `osm-…`) se consultan en OSM aunque haya
  // API: no existen en la base de datos de PostgreSQL.
  if (USE_API && !id.startsWith('osm-')) {
    return request<CafeDetail>(`/api/cafes/${id}`);
  }

  if (id.startsWith('osm-')) {
    const osmCafe = await queryCafeById(id);
    if (!osmCafe) {
      throw new Error('No se encontró la cafetería.');
    }

    return withStoredReviews(osmCafe);
  }

  throw new Error('No se encontró la cafetería.');
}

export async function createReview(
  cafeId: string,
  requestBody: CreateReviewRequest,
): Promise<Review> {
  // Las reseñas de cafés de OpenStreetMap se guardan en el navegador: la API
  // no los conoce y rechazaría el id.
  if (USE_API && !cafeId.startsWith('osm-')) {
    return request<Review>(`/api/cafes/${cafeId}/reviews`, {
      method: 'POST',
      body: JSON.stringify(requestBody),
    });
  }

  const review: Review = {
    id: crypto.randomUUID(),
    author: requestBody.author.trim(),
    rating: requestBody.rating,
    comment: requestBody.comment.trim(),
    createdAt: new Date().toISOString(),
    itemRatings: requestBody.itemRatings.map((rating) => ({
      menuItemId: rating.menuItemId,
      menuItemName: 'Plato',
      score: rating.score,
    })),
  };

  const stored = getStoredReviews();
  stored[cafeId] = [...(stored[cafeId] ?? []), review];
  saveStoredReviews(stored);

  return review;
}
