import { useQuery } from '@tanstack/react-query';
import { fetchCafes } from '../api/cafes';
import type { Coordinates } from '../lib/geo';

export function useCafes(location: Coordinates | null = null) {
  return useQuery({
    queryKey: ['cafes', location?.latitude ?? null, location?.longitude ?? null],
    queryFn: () => fetchCafes(location),
    staleTime: 1000 * 60 * 5,
  });
}
