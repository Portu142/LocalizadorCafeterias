import { useEffect } from 'react';
import { API_URL, USE_API } from '../config';
import { randomId } from '../lib/utils';

const VISITOR_KEY = 'lc_visita';
const INTERVAL_MS = 30_000;

/**
 * Id aleatorio de esta sesión del navegador. Se guarda en sessionStorage, así
 * que desaparece al cerrar el navegador: no sirve para seguir a nadie entre
 * visitas y no contiene datos personales.
 */
function visitorId(): string {
  try {
    const stored = sessionStorage.getItem(VISITOR_KEY);
    if (stored) {
      return stored;
    }

    const id = randomId();
    sessionStorage.setItem(VISITOR_KEY, id);
    return id;
  } catch {
    // Sin almacenamiento (modo privado estricto): un id por carga de página.
    return randomId();
  }
}

/**
 * Avisa a la API de que hay alguien usando la web (para «En vivo» del panel
 * de administración). Solo con la API, cada 30 s y mientras la pestaña está
 * visible; los fallos se ignoran porque no afectan a quien navega.
 */
export function usePresence(cafeId: string | null, cafeName: string | null) {
  useEffect(() => {
    if (!USE_API) {
      return;
    }

    const id = visitorId();

    const beat = () => {
      if (document.visibilityState !== 'visible') {
        return;
      }

      void fetch(`${API_URL}/api/presence`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        keepalive: true,
        body: JSON.stringify({ visitorId: id, cafeId, cafeName }),
      }).catch(() => undefined);
    };

    // Pequeña espera: al cargar, la cafetería seleccionada cambia varias veces
    // en un segundo y sin ella se enviaban varios latidos seguidos.
    const first = window.setTimeout(beat, 1_000);
    const timer = window.setInterval(beat, INTERVAL_MS);
    const onVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        beat();
      }
    };
    document.addEventListener('visibilitychange', onVisibilityChange);

    return () => {
      window.clearTimeout(first);
      window.clearInterval(timer);
      document.removeEventListener('visibilitychange', onVisibilityChange);
    };
  }, [cafeId, cafeName]);
}
