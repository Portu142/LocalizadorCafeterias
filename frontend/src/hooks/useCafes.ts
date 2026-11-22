import { useQuery } from '@tanstack/react-query';
import { fetchCafesWithSource, type DemoSource } from '../api/cafes';
import type { Coordinates } from '../lib/geo';

type UseCafesOptions = {
  /** Permite posponer la consulta (p. ej. mientras se pide la ubicación). */
  enabled?: boolean;
  /** Solo en la demo sin API: ejemplos con menú u OpenStreetMap real. */
  demoSource?: DemoSource;
};

export function useCafes(
  location: Coordinates | null = null,
  { enabled = true, demoSource = 'sample' }: UseCafesOptions = {},
) {
  return useQuery({
    queryKey: [
      'cafes',
      location?.latitude ?? null,
      location?.longitude ?? null,
      demoSource,
    ],
    queryFn: () => fetchCafesWithSource(location, demoSource),
    staleTime: 1000 * 60 * 5,
    // Con OpenStreetMap cada intento ya prueba varios endpoints; un único
    // reintento automático evita pantallas de carga de minutos.
    retry: 1,
    enabled,
  });
}
