import { Heart } from 'lucide-react';
import { cn } from '../lib/utils';

type NeighborhoodFilterProps = {
  neighborhoods: string[];
  active: string;
  onChange: (value: string) => void;
  showFavorites: boolean;
  onToggleFavorites: () => void;
  favoritesCount: number;
};

export function NeighborhoodFilter({
  neighborhoods,
  active,
  onChange,
  showFavorites,
  onToggleFavorites,
  favoritesCount,
}: NeighborhoodFilterProps) {
  return (
    <div
      className="flex flex-wrap items-center gap-2"
      role="group"
      aria-label="Filtros de cafeterías"
    >
      {neighborhoods.map((neighborhood) => (
        <button
          key={neighborhood}
          type="button"
          onClick={() => onChange(neighborhood)}
          aria-pressed={active === neighborhood}
          className={cn(
            'rounded-full border px-3.5 py-2 text-xs font-semibold transition',
            active === neighborhood
              ? 'border-coffee-900 bg-coffee-900 text-cream-50'
              : 'border-coffee-200 bg-white/70 text-coffee-700 hover:border-coffee-300 hover:bg-white',
          )}
        >
          {neighborhood}
        </button>
      ))}

      <button
        type="button"
        onClick={onToggleFavorites}
        aria-pressed={showFavorites}
        className={cn(
          'inline-flex items-center gap-1.5 rounded-full border px-3.5 py-2 text-xs font-semibold transition',
          showFavorites
            ? 'border-rose-300 bg-rose-50 text-rose-700'
            : 'border-coffee-200 bg-white/70 text-coffee-700 hover:border-coffee-300 hover:bg-white',
        )}
      >
        <Heart
          className={cn(
            'h-3.5 w-3.5',
            showFavorites && 'fill-rose-500 text-rose-500',
          )}
        />
        Favoritos {favoritesCount > 0 && `(${favoritesCount})`}
      </button>
    </div>
  );
}
