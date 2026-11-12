import { useState } from 'react';

const STORAGE_KEY = 'localizador_cafes_favorites';

function readFavorites(): string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

export function useFavorites() {
  const [favorites, setFavorites] = useState<string[]>(readFavorites);

  const toggleFavorite = (id: string) => {
    setFavorites((current) => {
      const next = current.includes(id)
        ? current.filter((favorite) => favorite !== id)
        : [...current, id];

      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
      } catch {
        // Ignorar si el navegador bloquea localStorage.
      }

      return next;
    });
  };

  return {
    favorites,
    toggleFavorite,
    isFavorite: (id: string) => favorites.includes(id),
  };
}
