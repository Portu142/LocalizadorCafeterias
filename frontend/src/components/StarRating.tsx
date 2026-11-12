import { Star } from 'lucide-react';
import { cn } from '../lib/utils';

type StarRatingProps = {
  value: number;
  size?: 'sm' | 'md';
  onChange?: (value: number) => void;
};

export function StarRating({ value, size = 'sm', onChange }: StarRatingProps) {
  const iconClass = size === 'sm' ? 'h-3.5 w-3.5' : 'h-5 w-5';

  return (
    <div
      className="flex items-center gap-0.5"
      role={onChange ? 'group' : 'img'}
      aria-label={
        onChange
          ? 'Selecciona una puntuación de 1 a 5 estrellas'
          : `Valoración: ${value.toFixed(1).replace('.', ',')} de 5 estrellas`
      }
    >
      {[1, 2, 3, 4, 5].map((star) => {
        const filled = star <= Math.round(value);
        const content = (
          <Star
            className={cn(
              iconClass,
              filled ? 'fill-caramel-500 text-caramel-500' : 'text-coffee-200',
            )}
            aria-hidden="true"
          />
        );

        if (!onChange) {
          return <span key={star}>{content}</span>;
        }

        return (
          <button
            key={star}
            type="button"
            onClick={() => onChange(star)}
            className="rounded-full p-0.5 transition hover:scale-110 focus:outline-none focus-visible:ring-2 focus-visible:ring-caramel-500"
            aria-label={`${star} de 5 estrellas`}
            aria-pressed={star <= value}
          >
            {content}
          </button>
        );
      })}
    </div>
  );
}
