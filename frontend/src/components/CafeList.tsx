import { AlertCircle, Coffee, SearchX } from 'lucide-react';
import type { CafeSummary } from '../types/cafe';
import { CafeCard } from './CafeCard';

type CafeListProps = {
  cafes: CafeSummary[];
  selectedId: string | null;
  favorites: string[];
  distances?: Map<string, number>;
  isLoading: boolean;
  isError: boolean;
  emptyTitle?: string;
  emptyDescription?: string;
  sourceNote?: string;
  emptyActionLabel?: string;
  onSelect: (id: string) => void;
  onToggleFavorite: (cafe: CafeSummary) => void;
  onRetry: () => void;
  onClearFilters: () => void;
};

export function CafeList({
  cafes,
  selectedId,
  favorites,
  distances,
  isLoading,
  isError,
  emptyTitle = 'No hay resultados con esos filtros.',
  emptyDescription = 'Prueba con otro barrio o borra la búsqueda.',
  sourceNote = 'Datos de ejemplo de Madrid',
  emptyActionLabel = 'Limpiar filtros',
  onSelect,
  onToggleFavorite,
  onRetry,
  onClearFilters,
}: CafeListProps) {
  return (
    <section className="card flex h-full flex-col overflow-hidden">
      <div className="flex items-center justify-between gap-3 border-b border-coffee-100 px-5 py-4">
        <div>
          <h2 className="font-display text-xl font-semibold text-coffee-950">
            Resultados
          </h2>
          <p className="text-xs text-coffee-500" aria-live="polite">
            {isLoading
              ? 'Buscando cafeterías…'
              : `${cafes.length} cafetería${cafes.length === 1 ? '' : 's'}`}
          </p>
        </div>
        <span
          className="rounded-full bg-cream-100 px-3 py-1 text-xs font-semibold text-coffee-700"
          aria-hidden="true"
        >
          {isLoading ? '…' : cafes.length}
        </span>
      </div>

      <div className="relative flex-1 overflow-hidden">
        <div
          className="h-full overflow-y-auto p-3"
          aria-busy={isLoading}
        >
          {isLoading && (
            <div className="space-y-2" aria-hidden="true">
              {Array.from({ length: 4 }).map((_, index) => (
                <div
                  key={index}
                  className="animate-pulse rounded-2xl border border-coffee-100 p-4"
                >
                  <div className="h-4 w-2/3 rounded bg-coffee-100" />
                  <div className="mt-2 h-3 w-1/2 rounded bg-coffee-100" />
                  <div className="mt-3 h-3 w-3/4 rounded bg-coffee-100" />
                </div>
              ))}
            </div>
          )}

          {!isLoading && isError && (
            <div className="flex h-full flex-col items-center justify-center gap-3 px-6 text-center">
              <AlertCircle className="h-8 w-8 text-rose-500" aria-hidden="true" />
              <p className="text-sm font-medium text-coffee-800">
                No se pudieron cargar las cafeterías.
              </p>
              <p className="text-xs text-coffee-500">
                Revisa tu conexión e inténtalo de nuevo.
              </p>
              <button type="button" onClick={onRetry} className="btn-primary">
                Reintentar
              </button>
            </div>
          )}

          {!isLoading && !isError && cafes.length === 0 && (
            <div className="flex h-full flex-col items-center justify-center gap-3 px-6 text-center">
              <SearchX className="h-8 w-8 text-coffee-500" aria-hidden="true" />
              <p className="text-sm font-medium text-coffee-800">{emptyTitle}</p>
              <p className="text-xs text-coffee-500">{emptyDescription}</p>
              <button type="button" onClick={onClearFilters} className="btn-ghost">
                {emptyActionLabel}
              </button>
            </div>
          )}

          {!isLoading && !isError && cafes.length > 0 && (
            <div className="space-y-1">
              {cafes.map((cafe) => (
                <CafeCard
                  key={cafe.id}
                  cafe={cafe}
                  distanceKm={distances?.get(cafe.id)}
                  active={cafe.id === selectedId}
                  favorite={favorites.includes(cafe.id)}
                  onSelect={() => onSelect(cafe.id)}
                  onToggleFavorite={() => onToggleFavorite(cafe)}
                />
              ))}
            </div>
          )}
        </div>

        <div
          className="pointer-events-none absolute inset-x-0 bottom-0 h-10 bg-gradient-to-t from-white via-white/70 to-transparent"
          aria-hidden="true"
        />
      </div>

      <div className="border-t border-coffee-100 px-5 py-3 text-[11px] text-coffee-500">
        <span className="inline-flex items-center gap-1.5">
          <Coffee className="h-3 w-3" aria-hidden="true" /> {sourceNote}
        </span>
      </div>
    </section>
  );
}
