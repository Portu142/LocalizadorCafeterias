import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createReview, ensureApiCafeId } from '../api/cafes';
import type { CreateReviewRequest } from '../types/cafe';

/**
 * Publica una reseña. Con la API, si el café es de OpenStreetMap se guarda
 * antes en la base de datos y `onCafeImported` recibe su nuevo id para que la
 * ficha pase a mostrar la versión guardada.
 */
export function useCreateReview(cafeId: string, onCafeImported?: (newId: string) => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateReviewRequest) => {
      const targetId = await ensureApiCafeId(cafeId);
      await createReview(targetId, request);
      return targetId;
    },
    onSuccess: async (targetId) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['cafes'] }),
        queryClient.invalidateQueries({ queryKey: ['cafe', targetId] }),
        queryClient.invalidateQueries({ queryKey: ['my-reviews'] }),
      ]);
      if (targetId !== cafeId) {
        onCafeImported?.(targetId);
      }
    },
  });
}
