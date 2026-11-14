import { useQuery } from '@tanstack/react-query';
import { fetchCafe } from '../api/cafes';

export function useCafe(id: string | null) {
  return useQuery({
    queryKey: ['cafe', id],
    queryFn: () => fetchCafe(id as string),
    enabled: Boolean(id),
    staleTime: 1000 * 60 * 5,
    retry: 1,
  });
}
