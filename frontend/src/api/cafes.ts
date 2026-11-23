import { API_URL, USE_API } from '../config';
import { distanceKm, type Coordinates } from '../lib/geo';
import { queryPhotonCafes } from './photon';
import { enrichWithOsmTags, queryCafeById, queryOverpassNearby } from './overpass';
import type {
  CafeDetail,
  CafePhoto,
  CafeSummary,
  CreateReviewRequest,
  MenuItem,
  Review,
  User,
  UserReview,
} from '../types/cafe';

const MOCK_REVIEWS_KEY = 'localizador_cafes_mock_reviews';

function isStoredReview(value: unknown): value is Review {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const review = value as Partial<Review>;

  return (
    typeof review.id === 'string' &&
    typeof review.author === 'string' &&
    typeof review.comment === 'string' &&
    typeof review.createdAt === 'string' &&
    typeof review.rating === 'number' &&
    review.rating >= 1 &&
    review.rating <= 5 &&
    (review.itemRatings === undefined || Array.isArray(review.itemRatings))
  );
}

/**
 * Reseñas del modo demo guardadas en el navegador. localStorage es editable
 * por el usuario (o por otra versión de la app), así que se descarta todo lo
 * que no tenga la forma esperada en lugar de dejar que rompa la ficha.
 */
function getStoredReviews(): Record<string, Review[]> {
  try {
    const raw = localStorage.getItem(MOCK_REVIEWS_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : null;

    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return {};
    }

    const reviews: Record<string, Review[]> = {};
    for (const [cafeId, list] of Object.entries(parsed)) {
      if (Array.isArray(list)) {
        reviews[cafeId] = list.filter(isStoredReview);
      }
    }

    return reviews;
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
    tags: cafe.tags,
    hours: cafe.hours,
  };
}

/** Error de la API con su código HTTP (p. ej. 401 = sin sesión). */
export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const headers = new Headers(options?.headers);
  // Con FormData el navegador pone el Content-Type con su «boundary».
  if (!(options?.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers,
    // La sesión viaja en una cookie HttpOnly de la API.
    credentials: 'include',
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as
      | { detail?: string; title?: string; errors?: Record<string, string[]> }
      | null;
    const firstError = problem?.errors ? Object.values(problem.errors).flat()[0] : undefined;

    throw new ApiError(
      firstError ??
        problem?.detail ??
        problem?.title ??
        (response.status === 401
          ? 'Inicia sesión para continuar.'
          : response.status === 429
            ? 'Demasiados intentos. Espera un momento y vuelve a probar.'
            : 'No se pudo conectar con la API.'),
      response.status,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export type CafeSource = 'api' | 'osm' | 'mixed';

function normalizeName(value: string): string {
  return value
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, ' ')
    .trim();
}

/**
 * Cafés de OpenStreetMap que no están ya en la base de datos: fuera los que
 * se importaron (mismo `osmId`) y los de ejemplo que coinciden en nombre y
 * están a menos de 75 m (el mismo local registrado dos veces).
 */
function withoutDuplicates(osmCafes: CafeDetail[], apiCafes: CafeSummary[]): CafeDetail[] {
  const importedIds = new Set(apiCafes.map((cafe) => cafe.osmId && `osm-${cafe.osmId}`));

  return osmCafes.filter((osmCafe) => {
    if (importedIds.has(osmCafe.id)) {
      return false;
    }

    const name = normalizeName(osmCafe.name);
    return !apiCafes.some((apiCafe) => {
      const apiName = normalizeName(apiCafe.name);
      const sameName = name.includes(apiName) || apiName.includes(name);
      return sameName && distanceKm(osmCafe, apiCafe) * 1000 <= 75;
    });
  });
}

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

  if (cafes.length > 0) {
    // Photon solo trae nombre y dirección: servicios y horario para los filtros.
    return enrichWithOsmTags(cafes);
  }

  return queryOverpassNearby(location);
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

    // La base de datos solo tiene los cafés de ejemplo y los que los usuarios
    // han guardado al aportar su carta: se mezclan con los de OpenStreetMap de
    // la zona para que la lista no se quede en unos pocos locales.
    const [cafes, osmCafes] = await Promise.all([
      request<CafeSummary[]>(`/api/cafes${query ? `?${query}` : ''}`),
      location
        ? queryOpenStreetMap(location).catch(() => [] as CafeDetail[])
        : Promise.resolve([] as CafeDetail[]),
    ]);

    const extra = withoutDuplicates(osmCafes, cafes).map(withStoredReviews).map(toSummary);

    if (cafes.length === 0) {
      return { cafes: extra, source: 'osm' };
    }

    return {
      cafes: [...cafes, ...extra],
      source: extra.length > 0 ? 'mixed' : 'api',
    };
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

// ---------------------------------------------------------------------------
// Cuentas, fotos y «Mis reseñas» (solo con la API .NET).
// ---------------------------------------------------------------------------

/** Usuario con sesión iniciada, o `null` si no hay sesión o no hay API. */
export async function fetchCurrentUser(): Promise<User | null> {
  if (!USE_API) {
    return null;
  }

  try {
    return await request<User>('/api/auth/me');
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return null;
    }
    throw error;
  }
}

export function login(email: string, password: string): Promise<User> {
  return request<User>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}

export function register(
  email: string,
  password: string,
  displayName: string,
): Promise<User> {
  return request<User>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({ email, password, displayName }),
  });
}

export function logout(): Promise<void> {
  return request<void>('/api/auth/logout', { method: 'POST' });
}

export function fetchMyReviews(): Promise<UserReview[]> {
  return request<UserReview[]>('/api/me/reviews');
}

export function uploadPhoto(cafeId: string, file: File): Promise<CafePhoto> {
  const body = new FormData();
  body.append('photo', file);

  return request<CafePhoto>(`/api/cafes/${cafeId}/photos`, {
    method: 'POST',
    body,
  });
}

/** Las fotos subidas llegan con ruta relativa a la API (`/api/photos/…`). */
export function resolvePhotoUrl(url: string): string {
  return url.startsWith('/') ? `${API_URL}${url}` : url;
}


// ---------------------------------------------------------------------------
// Carta aportada por la comunidad (solo con la API y con sesión).
// ---------------------------------------------------------------------------

/** Guarda en la API un café de OpenStreetMap (o devuelve el ya guardado). */
export function importOsmCafe(osmId: string): Promise<CafeDetail> {
  return request<CafeDetail>('/api/cafes/import', {
    method: 'POST',
    body: JSON.stringify({ osmId }),
  });
}

export function addMenuItem(cafeId: string, name: string, price: number): Promise<MenuItem> {
  return request<MenuItem>(`/api/cafes/${cafeId}/menu`, {
    method: 'POST',
    body: JSON.stringify({ name, price }),
  });
}

export function deleteMenuItem(cafeId: string, itemId: string): Promise<void> {
  return request<void>(`/api/cafes/${cafeId}/menu/${itemId}`, { method: 'DELETE' });
}

/**
 * Id del café en la base de datos de la API. Si es de OpenStreetMap, lo guarda
 * primero (el servidor descarga sus datos de OSM) para que reseñas, fotos y
 * platos queden en la base de datos y los vea el panel de administración.
 */
export async function ensureApiCafeId(cafeId: string): Promise<string> {
  if (USE_API && cafeId.startsWith('osm-')) {
    return (await importOsmCafe(cafeId)).id;
  }
  return cafeId;
}
