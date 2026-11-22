import { Heart, MapPin, Navigation, Star } from 'lucide-react';
import { formatDistance } from '../lib/geo';
import { isOpenAt } from '../lib/openingHours';
import { cn } from '../lib/utils';
import type { CafeSummary } from '../types/cafe';

type CafeCardProps = {
  cafe: CafeSummary;
  distanceKm?: number;
  active: boolean;
  favorite: boolean;
  onSelect: () => void;
  onToggleFavorite: () => void;
};

export function CafeCard({
  cafe,
  distanceKm,
  active,
  favorite,
  onSelect,
  onToggleFavorite,
}: CafeCardProps) {
  const open = isOpenAt(cafe.hours);

  return (
    <article
      className={cn(
        'group relative rounded-2xl border transition',
        active
          ? 'border-caramel-400 bg-cream-50 shadow-sm'
          : 'border-transparent hover:border-coffee-200 hover:bg-cream-50',
      )}
    >
      <div className="flex items-start gap-3 p-4">
        <button
          type="button"
          onClick={onSelect}
          aria-current={active ? 'true' : undefined}
          className="min-w-0 flex-1 rounded-lg text-left focus:outline-none focus-visible:ring-2 focus-visible:ring-caramel-500 focus-visible:ring-offset-2"
        >
          <h3 className="truncate font-semibold text-coffee-950">{cafe.name}</h3>
          <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-coffee-500">
            {typeof distanceKm === 'number' && (
              <span className="inline-flex items-center gap-1 rounded-full bg-caramel-500/10 px-2 py-0.5 font-semibold text-caramel-700">
                <Navigation className="h-3 w-3" aria-hidden="true" />
                {formatDistance(distanceKm)}
              </span>
            )}
            {open !== null && (
              <span
                className={cn(
                  'rounded-full px-2 py-0.5 font-semibold',
                  open ? 'bg-emerald-50 text-emerald-800' : 'bg-cream-200 text-coffee-600',
                )}
              >
                {open ? 'Abierto' : 'Cerrado'}
              </span>
            )}
            {cafe.reviewCount > 0 ? (
              <>
                <span className="inline-flex items-center gap-1 font-semibold text-caramel-600">
                  <Star
                    className="h-3.5 w-3.5 fill-caramel-500 text-caramel-500"
                    aria-hidden="true"
                  />
                  {cafe.rating.toFixed(1).replace('.', ',')}
                </span>
                <span>({cafe.reviewCount})</span>
              </>
            ) : (
              <span className="inline-flex items-center gap-1 font-medium text-coffee-500">
                <Star className="h-3.5 w-3.5 text-coffee-300" aria-hidden="true" />
                Sin reseñas
              </span>
            )}
            {cafe.neighborhood && (
              <>
                <span className="text-coffee-300" aria-hidden="true">
                  ·
                </span>
                <span className="inline-flex items-center gap-1">
                  <MapPin className="h-3 w-3" aria-hidden="true" />
                  {cafe.neighborhood}
                </span>
              </>
            )}
          </div>
          <p className="mt-2 line-clamp-1 text-xs text-coffee-500">{cafe.address}</p>
        </button>

        <button
          type="button"
          onClick={onToggleFavorite}
          aria-pressed={favorite}
          className={cn(
            'rounded-full p-2 transition',
            favorite
              ? 'bg-rose-50 text-rose-500'
              : 'text-coffee-500 hover:bg-cream-100 hover:text-coffee-700',
          )}
          aria-label={
            favorite
              ? `Quitar ${cafe.name} de favoritos`
              : `Añadir ${cafe.name} a favoritos`
          }
        >
          <Heart
            className={cn('h-4 w-4', favorite && 'fill-rose-500')}
            aria-hidden="true"
          />
        </button>
      </div>
    </article>
  );
}
