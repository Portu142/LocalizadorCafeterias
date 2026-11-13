import { useCallback, useState } from 'react';
import type { Coordinates } from '../lib/geo';

export type GeolocationStatus =
  | 'idle'
  | 'requesting'
  | 'granted'
  | 'denied'
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

export function useGeolocation(): GeolocationState {
  const [state, setState] = useState<GeolocationValue>({
    status: 'idle',
    coordinates: null,
    message: null,
  });

  const request = useCallback(() => {
    if (typeof navigator === 'undefined' || !('geolocation' in navigator)) {
      setState({
        status: 'unavailable',
        coordinates: null,
        message: 'Tu navegador no permite obtener la ubicación del dispositivo.',
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
      (error) => setState(describeError(error)),
      {
        enableHighAccuracy: false,
        timeout: 10_000,
        maximumAge: 5 * 60 * 1000,
      },
    );
  }, []);

  const clear = useCallback(() => {
    setState({ status: 'idle', coordinates: null, message: null });
  }, []);

  return { ...state, request, clear };
}
