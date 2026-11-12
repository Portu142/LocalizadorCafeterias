import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createReview } from '../api/cafes';
import type { CreateReviewRequest } from '../types/cafe';

export function useCreateReview(cafeId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateReviewRequest) => createReview(cafeId, request),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['cafes'] }),
        queryClient.invalidateQueries({ queryKey: ['cafe', cafeId] }),
      ]);
    },
  });
}
