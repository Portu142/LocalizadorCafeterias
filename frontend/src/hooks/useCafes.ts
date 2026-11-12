import { useQuery } from '@tanstack/react-query';
import { fetchCafes } from '../api/cafes';

export function useCafes() {
  return useQuery({
    queryKey: ['cafes'],
    queryFn: fetchCafes,
    staleTime: 1000 * 60 * 5,
  });
}
