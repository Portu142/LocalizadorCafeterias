import { useState } from 'react';
import {
  AlertCircle,
  Clock,
  ExternalLink,
  Heart,
  MapPin,
  MessageSquare,
  Navigation,
  Phone,
  UtensilsCrossed,
} from 'lucide-react';
import { formatDistance } from '../lib/geo';
import { cn, formatDate, formatPrice, initials } from '../lib/utils';
import type { CafeDetail as CafeDetailData } from '../types/cafe';
import { ReviewForm } from './ReviewForm';
import { StarRating } from './StarRating';

type CafeDetailProps = {
  cafe: CafeDetailData | undefined;
  distanceKm?: number;
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  isFavorite: boolean;
  onToggleFavorite: () => void;
};

export function CafeDetail({
  cafe,
  distanceKm,
  isLoading,
  isError,
  onRetry,
  isFavorite,
  onToggleFavorite,
}: CafeDetailProps) {
  const [activeTab, setActiveTab] = useState<'menu' | 'reviews'>('menu');

  if (isLoading) {
    return <DetailSkeleton />;
  }

  if (isError) {
    return (
      <section className="card flex h-full flex-col items-center justify-center gap-3 p-8 text-center">
        <AlertCircle className="h-9 w-9 text-rose-500" />
        <h2 className="font-display text-xl font-semibold text-coffee-950">
          No se pudo cargar la ficha
        </h2>
        <p className="text-sm text-coffee-500">
          Inténtalo de nuevo en unos segundos.
        </p>
        <button type="button" onClick={onRetry} className="btn-primary">
          Reintentar
        </button>
      </section>
    );
  }

  if (!cafe) {
    return (
      <section className="card flex h-full flex-col items-center justify-center gap-3 p-8 text-center">
        <span className="text-4xl">☕</span>
        <h2 className="font-display text-xl font-semibold text-coffee-950">
          Selecciona una cafetería
        </h2>
        <p className="max-w-xs text-sm text-coffee-500">
          Haz clic en una cafetería del listado o en un marcador del mapa para ver
          su menú, horario y reseñas.
        </p>
      </section>
    );
  }

  const mapsUrl = `https://www.google.com/maps/search/?api=1&query=${cafe.latitude},${cafe.longitude}`;
  // Sin menú (p. ej. cafés de OpenStreetMap) solo se muestra la pestaña de reseñas.
  const hasMenu = cafe.menu.length > 0;
  const visibleTab = hasMenu ? activeTab : 'reviews';

  return (
    <section className="card flex h-full flex-col overflow-hidden">
      <div className="border-b border-coffee-100 p-5">
        <div className="flex items-start justify-between gap-3">
          <div>
            {cafe.neighborhood && (
              <span className="inline-flex rounded-full bg-cream-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.12em] text-coffee-500">
                {cafe.neighborhood}
              </span>
            )}
            <h2 className="mt-2 font-display text-2xl font-semibold leading-tight text-coffee-950">
              {cafe.name}
            </h2>
            {cafe.reviewCount > 0 ? (
              <div className="mt-2 flex items-center gap-2">
                <StarRating value={cafe.rating} />
                <span className="text-sm font-semibold text-coffee-900">
                  {cafe.rating.toFixed(1).replace('.', ',')}
                </span>
                <span className="text-xs text-coffee-500">
                  ({cafe.reviewCount} reseñas)
                </span>
              </div>
            ) : (
              <p className="mt-2 text-xs text-coffee-500">Sin reseñas todavía</p>
            )}
          </div>

          <button
            type="button"
            onClick={onToggleFavorite}
            aria-pressed={isFavorite}
            aria-label={isFavorite ? 'Quitar de favoritos' : 'Añadir a favoritos'}
            className={cn(
              'rounded-full p-2.5 transition',
              isFavorite
                ? 'bg-rose-50 text-rose-500'
                : 'bg-cream-100 text-coffee-500 hover:text-coffee-700',
            )}
          >
            <Heart
              className={cn('h-5 w-5', isFavorite && 'fill-rose-500')}
              aria-hidden="true"
            />
          </button>
        </div>

        <div className="mt-4 space-y-2 text-sm text-coffee-600">
          <p className="flex items-start gap-2">
            <MapPin className="mt-0.5 h-4 w-4 shrink-0 text-caramel-500" />
            {cafe.address}
          </p>
          {cafe.hours && (
            <p className="flex items-center gap-2">
              <Clock className="h-4 w-4 shrink-0 text-caramel-500" />
              {cafe.hours}
            </p>
          )}
          {cafe.phone && (
            <p className="flex items-center gap-2">
              <Phone className="h-4 w-4 shrink-0 text-caramel-500" />
              {cafe.phone}
            </p>
          )}
          {typeof distanceKm === 'number' && (
            <p className="flex items-center gap-2 font-medium text-coffee-800">
              <Navigation className="h-4 w-4 shrink-0 text-caramel-500" aria-hidden="true" />
              Estás a {formatDistance(distanceKm)} de aquí
            </p>
          )}
        </div>

        <a
          href={mapsUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="mt-4 inline-flex items-center gap-1.5 text-xs font-semibold text-caramel-600 transition hover:text-caramel-500"
        >
          Cómo llegar <ExternalLink className="h-3.5 w-3.5" aria-hidden="true" />
        </a>
      </div>

      <div
        className="flex border-b border-coffee-100 px-3 pt-2"
        role="tablist"
        aria-label="Información de la cafetería"
      >
        {hasMenu && (
          <button
            type="button"
            role="tab"
            id="tab-menu"
            aria-selected={visibleTab === 'menu'}
            aria-controls="panel-menu"
            tabIndex={visibleTab === 'menu' ? 0 : -1}
            onClick={() => setActiveTab('menu')}
            onKeyDown={(event) => {
              if (event.key === 'ArrowRight') {
                setActiveTab('reviews');
                document.getElementById('tab-reviews')?.focus();
              }
            }}
            className={cn(
              'inline-flex items-center gap-2 rounded-t-xl px-3 py-2 text-sm font-semibold transition',
              visibleTab === 'menu'
                ? 'border-b-2 border-caramel-500 text-coffee-950'
                : 'text-coffee-500 hover:text-coffee-800',
            )}
          >
            <UtensilsCrossed className="h-4 w-4" aria-hidden="true" /> Menú
          </button>
        )}
        <button
          type="button"
          role="tab"
          id="tab-reviews"
          aria-selected={visibleTab === 'reviews'}
          aria-controls="panel-reviews"
          tabIndex={visibleTab === 'reviews' ? 0 : -1}
          onClick={() => setActiveTab('reviews')}
          onKeyDown={(event) => {
            if (event.key === 'ArrowLeft' && hasMenu) {
              setActiveTab('menu');
              document.getElementById('tab-menu')?.focus();
            }
          }}
          className={cn(
            'inline-flex items-center gap-2 rounded-t-xl px-3 py-2 text-sm font-semibold transition',
            visibleTab === 'reviews'
              ? 'border-b-2 border-caramel-500 text-coffee-950'
              : 'text-coffee-500 hover:text-coffee-800',
          )}
        >
          <MessageSquare className="h-4 w-4" aria-hidden="true" /> Reseñas ({cafe.reviewCount})
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-5">
        {hasMenu && (
          <div
            id="panel-menu"
            role="tabpanel"
            aria-labelledby="tab-menu"
            hidden={visibleTab !== 'menu'}
          >
            <h3 className="text-xs font-semibold uppercase tracking-[0.14em] text-coffee-500">
              Menú destacado
            </h3>
            <ul className="mt-3 divide-y divide-coffee-100">
              {cafe.menu.map((item) => (
                <li
                  key={item.id}
                  className="flex items-center justify-between gap-4 py-3"
                >
                  <span className="text-sm text-coffee-800">{item.name}</span>
                  <span className="shrink-0 text-sm font-semibold text-coffee-950">
                    {formatPrice(item.price)}
                  </span>
                </li>
              ))}
            </ul>

            <div className="mt-6 rounded-2xl bg-cream-100 p-4 text-xs leading-5 text-coffee-600">
              Precios orientativos. El menú puede variar según la temporada.
            </div>
          </div>
        )}

        <div
          id="panel-reviews"
          role="tabpanel"
          aria-labelledby="tab-reviews"
          hidden={visibleTab !== 'reviews'}
        >
          <div className="space-y-3">
            {cafe.reviews.length === 0 ? (
              <p className="rounded-2xl bg-cream-100 p-4 text-sm text-coffee-600">
                Todavía no hay reseñas. ¡Sé el primero en opinar!
              </p>
            ) : (
              cafe.reviews.map((review) => (
                <article
                  key={review.id}
                  className="rounded-2xl border border-coffee-100 bg-cream-50 p-4"
                >
                  <div className="flex items-center justify-between gap-3">
                    <div className="flex items-center gap-2">
                      <span className="flex h-8 w-8 items-center justify-center rounded-full bg-coffee-800 text-xs font-semibold text-cream-50">
                        {initials(review.author)}
                      </span>
                      <div>
                        <p className="text-sm font-semibold text-coffee-950">
                          {review.author}
                        </p>
                        <p className="text-[11px] text-coffee-500">
                          {formatDate(review.createdAt)}
                        </p>
                      </div>
                    </div>
                    <StarRating value={review.rating} />
                  </div>
                  <p className="mt-3 text-sm leading-6 text-coffee-700">
                    {review.comment}
                  </p>
                </article>
              ))
            )}
          </div>

          <ReviewForm cafeId={cafe.id} />
        </div>
      </div>
    </section>
  );
}

function DetailSkeleton() {
  return (
    <section className="card flex h-full flex-col overflow-hidden">
      <div className="animate-pulse border-b border-coffee-100 p-5">
        <div className="h-5 w-24 rounded bg-coffee-100" />
        <div className="mt-3 h-8 w-2/3 rounded bg-coffee-100" />
        <div className="mt-3 h-4 w-40 rounded bg-coffee-100" />
        <div className="mt-5 space-y-2">
          <div className="h-4 w-full rounded bg-coffee-100" />
          <div className="h-4 w-5/6 rounded bg-coffee-100" />
          <div className="h-4 w-2/3 rounded bg-coffee-100" />
        </div>
      </div>
      <div className="animate-pulse space-y-3 p-5">
        <div className="h-4 w-32 rounded bg-coffee-100" />
        <div className="h-10 w-full rounded bg-coffee-100" />
        <div className="h-10 w-full rounded bg-coffee-100" />
        <div className="h-10 w-full rounded bg-coffee-100" />
      </div>
    </section>
  );
}
