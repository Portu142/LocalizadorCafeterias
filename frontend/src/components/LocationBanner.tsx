import { LocateFixed, MapPin } from 'lucide-react';
import type { GeolocationStatus } from '../hooks/useGeolocation';

type LocationBannerProps = {
  status: GeolocationStatus;
  onRequest: () => void;
};

/**
 * Aviso con acción explícita para pedir la ubicación. Es el plan B cuando el
 * navegador bloquea la petición automática (Chrome aplica "quiet blocks" si se
 * ignora el aviso o si no hay gesto del usuario).
 */
export function LocationBanner({ status, onRequest }: LocationBannerProps) {
  // 'blocked': el permiso sigue en "preguntar" y basta con un clic.
  // 'denied'/'unavailable'/'error': hay que revisar los ajustes del navegador.
  const needsSettings =
    status === 'denied' || status === 'unavailable' || status === 'error';
  const label = needsSettings ? 'Reintentar ubicación' : 'Permitir ubicación';

  return (
    <div className="mb-4 flex flex-col gap-3 rounded-2xl border border-caramel-300/60 bg-caramel-500/5 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-start gap-2.5">
        <span className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-caramel-500/15 text-caramel-700">
          <MapPin className="h-4 w-4" aria-hidden="true" />
        </span>
        <div className="text-sm">
          <p className="font-semibold text-coffee-900">
            Muestra cafeterías cerca de ti
          </p>
          <p className="mt-0.5 text-xs leading-5 text-coffee-600">
            {needsSettings
              ? 'El navegador ha bloqueado la ubicación. Ábrela desde el candado de la barra de direcciones y vuelve a intentarlo.'
              : 'Concede la ubicación y la app cargará automáticamente los cafés de tu zona ordenados por distancia.'}
          </p>
        </div>
      </div>

      <button
        type="button"
        onClick={onRequest}
        className="inline-flex shrink-0 items-center justify-center gap-2 rounded-full bg-coffee-900 px-4 py-2 text-xs font-semibold text-cream-50 transition hover:bg-coffee-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-caramel-500 focus-visible:ring-offset-2"
      >
        <LocateFixed className="h-3.5 w-3.5" aria-hidden="true" />
        {label}
      </button>
    </div>
  );
}
