export type Coordinates = {
  latitude: number;
  longitude: number;
};

const EARTH_RADIUS_KM = 6371;

function toRadians(value: number): number {
  return (value * Math.PI) / 180;
}

/** Distancia en kilómetros entre dos puntos (fórmula de Haversine). */
export function distanceKm(from: Coordinates, to: Coordinates): number {
  const dLat = toRadians(to.latitude - from.latitude);
  const dLng = toRadians(to.longitude - from.longitude);
  const lat1 = toRadians(from.latitude);
  const lat2 = toRadians(to.latitude);

  const haversine =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) ** 2;

  return 2 * EARTH_RADIUS_KM * Math.asin(Math.sqrt(haversine));
}

/** Formato corto en español: "450 m", "1,2 km", "12 km". */
export function formatDistance(km: number): string {
  if (km < 1) {
    return `${Math.round(km * 1000)} m`;
  }

  if (km < 10) {
    return `${km.toFixed(1).replace('.', ',')} km`;
  }

  return `${Math.round(km)} km`;
}

/** Ubicación predeterminada (Sol, Madrid) que se usa cuando el usuario aún no
 *  ha activado “Cerca de mí”. Evita mostrar datos de ejemplo estáticos y en su
 *  lugar carga cafeterías reales de OpenStreetMap cercanas a ese punto. */
export const DEFAULT_LOCATION: Coordinates = {
  latitude: 40.41686,
  longitude: -3.70379,
};
