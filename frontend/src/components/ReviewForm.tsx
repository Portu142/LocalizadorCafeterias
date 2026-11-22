import { useRef, useState, type FormEvent } from 'react';
import { CheckCircle2, Loader2, Send } from 'lucide-react';
import { useCreateReview } from '../hooks/useCreateReview';
import type { MenuItem } from '../types/cafe';
import { StarRating } from './StarRating';

type ReviewFormProps = {
  cafeId: string;
  menu: MenuItem[];
};

type FieldErrors = {
  author?: string;
  comment?: string;
};

export function ReviewForm({ cafeId, menu }: ReviewFormProps) {
  const [author, setAuthor] = useState('');
  const [rating, setRating] = useState(5);
  const [itemScores, setItemScores] = useState<Record<string, number>>({});
  const [comment, setComment] = useState('');
  const [errors, setErrors] = useState<FieldErrors>({});
  const [success, setSuccess] = useState(false);
  const authorRef = useRef<HTMLInputElement>(null);
  const commentRef = useRef<HTMLTextAreaElement>(null);
  const mutation = useCreateReview(cafeId);

  const validate = () => {
    const nextErrors: FieldErrors = {};

    if (!author.trim()) {
      nextErrors.author = 'Escribe tu nombre para publicar la reseña.';
    } else if (author.trim().length < 2) {
      nextErrors.author = 'El nombre debe tener al menos 2 caracteres.';
    }

    if (!comment.trim()) {
      nextErrors.comment = 'Escribe un comentario sobre tu experiencia.';
    } else if (comment.trim().length < 10) {
      nextErrors.comment = 'El comentario debe tener al menos 10 caracteres.';
    }

    setErrors(nextErrors);

    if (nextErrors.author) {
      authorRef.current?.focus();
    } else if (nextErrors.comment) {
      commentRef.current?.focus();
    }

    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSuccess(false);

    if (!validate()) {
      return;
    }

    mutation.mutate(
      {
        author: author.trim(),
        rating,
        comment: comment.trim(),
        // Solo se envían los platos que el usuario ha puntuado.
        itemRatings: menu
          .filter((item) => (itemScores[item.id] ?? 0) > 0)
          .map((item) => ({ menuItemId: item.id, score: itemScores[item.id] ?? 0 })),
      },
      {
        onSuccess: () => {
          setAuthor('');
          setRating(5);
          setItemScores({});
          setComment('');
          setErrors({});
          setSuccess(true);
        },
      },
    );
  };

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      className="mt-6 rounded-2xl border border-coffee-100 bg-white p-4"
    >
      <h3 className="text-sm font-semibold text-coffee-950">Deja tu reseña</h3>
      <p className="mt-1 text-xs text-coffee-600">
        Comparte tu experiencia con la comunidad.
      </p>

      <div className="mt-4 space-y-3">
        <div>
          <label
            htmlFor="review-author"
            className="mb-1 block text-xs font-medium text-coffee-700"
          >
            Tu nombre
          </label>
          <input
            id="review-author"
            ref={authorRef}
            value={author}
            onChange={(event) => {
              setAuthor(event.target.value);
              setSuccess(false);
              if (mutation.isError) {
                mutation.reset();
              }
              if (errors.author) {
                setErrors((current) => ({ ...current, author: undefined }));
              }
            }}
            maxLength={80}
            autoComplete="name"
            required
            aria-invalid={Boolean(errors.author)}
            aria-describedby={errors.author ? 'review-author-error' : undefined}
            className="w-full rounded-xl border border-coffee-200 bg-cream-50 px-3 py-2 text-sm text-coffee-950 outline-none transition placeholder:text-coffee-500 focus:border-caramel-400 focus:ring-2 focus:ring-caramel-400/20 aria-[invalid=true]:border-rose-400"
            placeholder="Ej. Ana"
          />
          {errors.author && (
            <p id="review-author-error" className="mt-1 text-xs text-rose-700">
              {errors.author}
            </p>
          )}
        </div>

        <div>
          <span className="mb-1 block text-xs font-medium text-coffee-700">
            Puntuación general
          </span>
          <StarRating value={rating} size="md" label="general" onChange={setRating} />
        </div>

        {menu.length > 0 && (
          <fieldset>
            <legend className="mb-1 block text-xs font-medium text-coffee-700">
              Puntuación del menú <span className="font-normal text-coffee-500">(opcional)</span>
            </legend>
            <p className="mb-2 text-[11px] text-coffee-500">
              Puntúa solo los platos que hayas probado.
            </p>
            <ul className="space-y-1.5">
              {menu.map((item) => {
                const score = itemScores[item.id] ?? 0;

                return (
                  <li
                    key={item.id}
                    className="flex items-center justify-between gap-3 rounded-xl bg-cream-50 px-3 py-2"
                  >
                    <span className="text-xs text-coffee-800">{item.name}</span>
                    <span className="flex items-center gap-1.5">
                      <StarRating
                        value={score}
                        label={`del plato ${item.name}`}
                        onChange={(value) =>
                          setItemScores((current) => ({
                            ...current,
                            [item.id]: current[item.id] === value ? 0 : value,
                          }))
                        }
                      />
                      {score > 0 && (
                        <button
                          type="button"
                          onClick={() =>
                            setItemScores((current) => ({ ...current, [item.id]: 0 }))
                          }
                          className="px-1 py-1 text-[11px] font-medium text-coffee-600 underline-offset-2 hover:text-coffee-800 hover:underline"
                          aria-label={`Quitar la puntuación de ${item.name}`}
                        >
                          Quitar
                        </button>
                      )}
                    </span>
                  </li>
                );
              })}
            </ul>
          </fieldset>
        )}

        <div>
          <div className="mb-1 flex items-center justify-between gap-2">
            <label htmlFor="review-comment" className="block text-xs font-medium text-coffee-700">
              Comentario
            </label>
            <span id="review-comment-count" className="text-[11px] text-coffee-500">
              {comment.length}/500
            </span>
          </div>
          <textarea
            id="review-comment"
            ref={commentRef}
            value={comment}
            onChange={(event) => {
              setComment(event.target.value);
              setSuccess(false);
              if (mutation.isError) {
                mutation.reset();
              }
              if (errors.comment) {
                setErrors((current) => ({ ...current, comment: undefined }));
              }
            }}
            maxLength={500}
            required
            rows={3}
            aria-invalid={Boolean(errors.comment)}
            aria-describedby={
              errors.comment
                ? 'review-comment-error review-comment-count'
                : 'review-comment-count'
            }
            className="w-full resize-none rounded-xl border border-coffee-200 bg-cream-50 px-3 py-2 text-sm text-coffee-950 outline-none transition placeholder:text-coffee-500 focus:border-caramel-400 focus:ring-2 focus:ring-caramel-400/20 aria-[invalid=true]:border-rose-400"
            placeholder="¿Qué te ha parecido el café?"
          />
          {errors.comment && (
            <p id="review-comment-error" className="mt-1 text-xs text-rose-700">
              {errors.comment}
            </p>
          )}
        </div>
      </div>

      {mutation.isError && (
        <p
          role="alert"
          className="mt-3 rounded-xl bg-rose-50 px-3 py-2 text-xs text-rose-700"
        >
          {mutation.error.message || 'No se pudo publicar la reseña. Inténtalo de nuevo.'}
        </p>
      )}

      {success && (
        <p
          role="status"
          className="mt-3 inline-flex items-center gap-1.5 rounded-xl bg-emerald-50 px-3 py-2 text-xs font-medium text-emerald-800"
        >
          <CheckCircle2 className="h-3.5 w-3.5" aria-hidden="true" /> ¡Gracias! Tu
          reseña se ha publicado.
        </p>
      )}

      <button
        type="submit"
        disabled={mutation.isPending}
        className="mt-4 inline-flex w-full items-center justify-center gap-2 rounded-xl bg-coffee-900 px-4 py-2.5 text-sm font-semibold text-cream-50 transition hover:bg-coffee-950 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {mutation.isPending ? (
          <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
        ) : (
          <Send className="h-4 w-4" aria-hidden="true" />
        )}
        {mutation.isPending ? 'Publicando…' : 'Publicar reseña'}
      </button>
    </form>
  );
}
