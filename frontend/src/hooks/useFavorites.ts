import { useState } from 'react';
import type { CafeSummary } from '../types/cafe';

const STORAGE_KEY = 'localizador_cafes_favorites';

function isCafeSummary(value: unknown): value is CafeSummary {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const cafe = value as Partial<CafeSummary>;

  return (
    typeof cafe.id === 'string' &&
    typeof cafe.name === 'string' &&
    typeof cafe.address === 'string' &&
    typeof cafe.latitude === 'number' &&
    typeof cafe.longitude === 'number'
  );
}

function readFavorites(): CafeSummary[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return [];
    }

    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) {
      return [];
    }

    // Se guardan fichas completas para que los favoritos no dependan de la
    // ubicación activa: al cambiar de zona siguen apareciendo. Los formatos
    // antiguos (solo ids) se descartan porque no permiten reconstruir la ficha.
    return parsed.filter(isCafeSummary);
  } catch {
    return [];
  }
}

export function useFavorites() {
  const [favorites, setFavorites] = useState<CafeSummary[]>(readFavorites);

  const toggleFavorite = (cafe: CafeSummary) => {
    // Se guarda solo la ficha resumida (sin menú ni reseñas) para no inflar
    // localStorage cuando el café llega como detalle completo.
    const summary: CafeSummary = {
      id: cafe.id,
      name: cafe.name,
      address: cafe.address,
      neighborhood: cafe.neighborhood,
      rating: cafe.rating,
      reviewCount: cafe.reviewCount,
      latitude: cafe.latitude,
      longitude: cafe.longitude,
    };

    setFavorites((current) => {
      const exists = current.some((favorite) => favorite.id === summary.id);
      const next = exists
        ? current.filter((favorite) => favorite.id !== summary.id)
        : [...current, summary];

      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
      } catch {
        // Ignorar si el navegador bloquea localStorage.
      }

      return next;
    });
  };

  return {
    /** Fichas completas de los favoritos, independientes de la ubicación. */
    favorites,
    favoriteIds: favorites.map((favorite) => favorite.id),
    toggleFavorite,
    isFavorite: (id: string) => favorites.some((favorite) => favorite.id === id),
  };
}
