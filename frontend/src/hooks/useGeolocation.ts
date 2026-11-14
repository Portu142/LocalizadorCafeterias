import { useCallback, useEffect, useRef, useState } from 'react';
import type { Coordinates } from '../lib/geo';

export type GeolocationStatus =
  | 'idle'
  | 'requesting'
  | 'granted'
  | 'denied'
  | 'blocked'
  | 'unavailable'
  | 'error';

type GeolocationValue = {
  status: GeolocationStatus;
  coordinates: Coordinates | null;
  message: string | null;
};

export type GeolocationState = GeolocationValue & {
  request: () => void;
  clear: () => void;
};

type UseGeolocationOptions = {
  /** Pide la ubicación automáticamente al montar (una sola vez por carga). */
  autoRequest?: boolean;
};

const PERMISSION_DENIED = 1;
const POSITION_UNAVAILABLE = 2;
const TIMEOUT = 3;

function describeError(error: GeolocationPositionError): GeolocationValue {
  if (error.code === PERMISSION_DENIED) {
    return {
      status: 'denied',
      coordinates: null,
      message:
        'No has permitido el acceso a tu ubicación. Puedes activarlo en los ajustes de tu navegador.',
    };
  }

  if (error.code === POSITION_UNAVAILABLE) {
    return {
      status: 'unavailable',
      coordinates: null,
      message: 'No se ha podido determinar tu ubicación. Inténtalo de nuevo.',
    };
  }

  if (error.code === TIMEOUT) {
    return {
      status: 'error',
      coordinates: null,
      message: 'La ubicación tardó demasiado. Inténtalo de nuevo.',
    };
  }

  return {
    status: 'error',
    coordinates: null,
    message: 'No se ha podido obtener tu ubicación. Inténtalo de nuevo.',
  };
}

/**
 * Estado del permiso según la Permissions API. Devuelve `null` cuando el
 * navegador no la soporta (Safari antiguo) o no permite consultarla.
 */
async function queryPermission(): Promise<PermissionState | null> {
  if (typeof navigator === 'undefined' || !navigator.permissions?.query) {
    return null;
  }

  try {
    const status = await navigator.permissions.query({
      name: 'geolocation' as PermissionName,
    });
    return status.state;
  } catch {
    return null;
  }
}

export function useGeolocation({
  autoRequest = false,
}: UseGeolocationOptions = {}): GeolocationState {
  const [state, setState] = useState<GeolocationValue>({
    status: 'idle',
    coordinates: null,
    message: null,
  });

  // Evita repetir la petición automática (incluye el doble montaje de
  // StrictMode y los re-renderizados posteriores).
  const autoRequested = useRef(false);

  const request = useCallback(() => {
    if (typeof navigator === 'undefined' || !('geolocation' in navigator)) {
      setState({
        status: 'unavailable',
        coordinates: null,
        message: 'Tu navegador no permite obtener la ubicación del dispositivo.',
      });
      return;
    }

    // Fuera de HTTPS (o localhost) los navegadores bloquean la geolocalización
    // sin ni siquiera mostrar el permiso.
    if (typeof window !== 'undefined' && !window.isSecureContext) {
      setState({
        status: 'unavailable',
        coordinates: null,
        message:
          'La ubicación solo funciona en HTTPS o en localhost. Abre la app desde una dirección segura.',
      });
      return;
    }

    setState({ status: 'requesting', coordinates: null, message: null });

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setState({
          status: 'granted',
          coordinates: {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
          },
          message: null,
        });
      },
      (error) => {
        if (error.code === PERMISSION_DENIED) {
          // Chrome puede denegar sin mostrar el aviso si no hay gesto del
          // usuario ("quiet block"). En ese caso el permiso sigue en "prompt"
          // y basta con reintentar desde un botón.
          void queryPermission().then((permission) => {
            setState(
              permission === 'prompt'
                ? {
                    status: 'blocked',
                    coordinates: null,
                    message:
                      'El navegador ha bloqueado la petición automática. Pulsa «Permitir ubicación» para concederla.',
                  }
                : describeError(error),
            );
          });
          return;
        }

        setState(describeError(error));
      },
      {
        enableHighAccuracy: false,
        timeout: 10_000,
        maximumAge: 5 * 60 * 1000,
      },
    );
  }, []);

  // Petición automática al cargar: si el permiso ya está concedido (o el
  // navegador permite preguntarlo) la app se centra en tu zona sin clics.
  useEffect(() => {
    if (!autoRequest || autoRequested.current) {
      return;
    }

    autoRequested.current = true;

    void queryPermission().then((permission) => {
      if (permission === 'denied') {
        setState({
          status: 'denied',
          coordinates: null,
          message:
            'La ubicación está bloqueada para este sitio. Ábrela desde el candado de la barra de direcciones y pulsa «Reintentar».',
        });
        return;
      }

      request();
    });
  }, [autoRequest, request]);

  // Si el usuario concede el permiso mientras la página está abierta (por
  // ejemplo desde los ajustes del navegador), se pide la ubicación al momento
  // y el mapa se recentra en su zona.
  useEffect(() => {
    if (typeof navigator === 'undefined' || !navigator.permissions?.query) {
      return;
    }

    let permissionStatus: PermissionStatus | null = null;
    let cancelled = false;
    let onChange: (() => void) | null = null;

    void navigator.permissions
      .query({ name: 'geolocation' as PermissionName })
      .then((permission) => {
        if (cancelled) {
          return;
        }

        permissionStatus = permission;
        onChange = () => {
          if (permission.state === 'granted') {
            request();
          }
        };
        permission.addEventListener('change', onChange);
      })
      .catch(() => {
        // Sin Permissions API seguimos con el flujo normal.
      });

    return () => {
      cancelled = true;
      if (permissionStatus && onChange) {
        permissionStatus.removeEventListener('change', onChange);
      }
    };
  }, [request]);

  const clear = useCallback(() => {
    setState({ status: 'idle', coordinates: null, message: null });
  }, []);

  return { ...state, request, clear };
}
