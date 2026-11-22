import { Loader2, MessageSquare } from 'lucide-react';
import { useMyReviews } from '../hooks/useAuth';
import { formatDate } from '../lib/utils';
import { Modal } from './Modal';
import { StarRating } from './StarRating';

type MyReviewsDialogProps = {
  open: boolean;
  onClose: () => void;
};

/** «Mis reseñas»: todas las reseñas publicadas con la cuenta actual. */
export function MyReviewsDialog({ open, onClose }: MyReviewsDialogProps) {
  const { data: reviews, isLoading, isError, refetch } = useMyReviews(open);

  return (
    <Modal open={open} title="Mis reseñas" onClose={onClose}>
      <div className="max-h-[60vh] overflow-y-auto pr-1" aria-busy={isLoading}>
        {isLoading && (
          <p className="flex items-center gap-2 text-sm text-coffee-600" role="status">
            <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            Cargando tus reseñas…
          </p>
        )}

        {isError && (
          <div className="space-y-2 text-sm text-coffee-700" role="alert">
            <p>No se pudieron cargar tus reseñas.</p>
            <button type="button" onClick={() => void refetch()} className="btn-ghost">
              Reintentar
            </button>
          </div>
        )}

        {reviews && reviews.length === 0 && (
          <div className="rounded-2xl bg-cream-100 p-5 text-center">
            <MessageSquare className="mx-auto h-6 w-6 text-coffee-400" aria-hidden="true" />
            <p className="mt-2 text-sm font-medium text-coffee-800">
              Todavía no has escrito ninguna reseña
            </p>
            <p className="mt-1 text-xs text-coffee-600">
              Abre la ficha de una cafetería y pulsa «Reseñas» para dejar la tuya.
            </p>
          </div>
        )}

        {reviews && reviews.length > 0 && (
          <ul className="space-y-3">
            {reviews.map((review) => (
              <li key={review.id} className="rounded-2xl border border-coffee-100 bg-white p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-sm font-semibold text-coffee-950">{review.cafeName}</p>
                    <p className="text-[11px] text-coffee-500">{formatDate(review.createdAt)}</p>
                  </div>
                  <StarRating value={review.rating} />
                </div>
                <p className="mt-2 text-sm leading-6 text-coffee-700">{review.comment}</p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </Modal>
  );
}
