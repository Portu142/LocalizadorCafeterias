import { API_URL } from '../config';
import { mockCafes } from '../data/mockCafes';
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

export async function fetchCafes(): Promise<CafeSummary[]> {
  if (API_URL) {
    return request<CafeSummary[]>('/api/cafes');
  }

  return mockCafes.map(withStoredReviews).map(toSummary);
}

export async function fetchCafe(id: string): Promise<CafeDetail> {
  if (API_URL) {
    return request<CafeDetail>(`/api/cafes/${id}`);
  }

  const cafe = mockCafes.find((item) => item.id === id);
  if (!cafe) {
    throw new Error('No se encontró la cafetería.');
  }

  return withStoredReviews(cafe);
}

export async function createReview(
  cafeId: string,
  requestBody: CreateReviewRequest,
): Promise<Review> {
  if (API_URL) {
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
  };

  const stored = getStoredReviews();
  stored[cafeId] = [...(stored[cafeId] ?? []), review];
  saveStoredReviews(stored);

  return review;
}
