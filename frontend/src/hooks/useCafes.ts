import { useQuery } from '@tanstack/react-query';
import { fetchCafes } from '../api/cafes';
import type { Coordinates } from '../lib/geo';

type UseCafesOptions = {
  /** Permite posponer la consulta (p. ej. mientras se pide la ubicación). */
  enabled?: boolean;
};

export function useCafes(
  location: Coordinates | null = null,
  { enabled = true }: UseCafesOptions = {},
) {
  return useQuery({
    queryKey: ['cafes', location?.latitude ?? null, location?.longitude ?? null],
    queryFn: () => fetchCafes(location),
    staleTime: 1000 * 60 * 5,
    // Con OpenStreetMap cada intento ya prueba varios endpoints; un único
    // reintento automático evita pantallas de carga de minutos.
    retry: 1,
    enabled,
  });
}
