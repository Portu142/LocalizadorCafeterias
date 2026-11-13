import { LocateFixed, Loader2, X } from 'lucide-react';
import { cn } from '../lib/utils';
import type { GeolocationStatus } from '../hooks/useGeolocation';

type LocationFilterProps = {
  status: GeolocationStatus;
  onRequest: () => void;
  onClear: () => void;
};

export function LocationFilter({ status, onRequest, onClear }: LocationFilterProps) {
  const isActive = status === 'granted';
  const isRequesting = status === 'requesting';

  return (
    <button
      type="button"
      onClick={isActive ? onClear : onRequest}
      aria-pressed={isActive}
      disabled={isRequesting}
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full border px-3.5 py-1.5 text-xs font-semibold transition disabled:cursor-wait disabled:opacity-70',
        isActive
          ? 'border-caramel-500 bg-caramel-500/10 text-caramel-700'
          : 'border-coffee-200 bg-white/70 text-coffee-700 hover:border-coffee-300 hover:bg-white',
      )}
    >
      {isRequesting ? (
        <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden="true" />
      ) : isActive ? (
        <X className="h-3.5 w-3.5" aria-hidden="true" />
      ) : (
        <LocateFixed className="h-3.5 w-3.5" aria-hidden="true" />
      )}
      {isRequesting
        ? 'Localizando...'
        : isActive
          ? 'Cerca de mí'
          : 'Usar mi ubicación'}
    </button>
  );
}
