import { useRef, type KeyboardEvent } from 'react';
import { Star } from 'lucide-react';
import { cn } from '../lib/utils';

type StarRatingProps = {
  value: number;
  size?: 'sm' | 'md';
  /** Etiqueta accesible para distinguir puntuaciones (p. ej. «Menú»). */
  label?: string;
  onChange?: (value: number) => void;
};

const STARS = [1, 2, 3, 4, 5];

export function StarRating({ value, size = 'sm', label, onChange }: StarRatingProps) {
  const iconClass = size === 'sm' ? 'h-3.5 w-3.5' : 'h-5 w-5';
  const formatted = value.toFixed(1).replace('.', ',');
  const groupRef = useRef<HTMLDivElement>(null);

  if (!onChange) {
    return (
      <div
        className="flex items-center gap-0.5"
        role="img"
        aria-label={
          label
            ? `Valoración ${label}: ${formatted} de 5 estrellas`
            : `Valoración: ${formatted} de 5 estrellas`
        }
      >
        {STARS.map((star) => (
          <Star
            key={star}
            className={cn(
              iconClass,
              star <= Math.round(value)
                ? 'fill-caramel-500 text-caramel-500'
                : 'text-coffee-200',
            )}
            aria-hidden="true"
          />
        ))}
      </div>
    );
  }

  // Grupo de opciones (patrón radiogroup): una sola parada de tabulación y
  // flechas para cambiar la puntuación, como un <input type="radio">.
  const focusStar = (star: number) => {
    groupRef.current
      ?.querySelector<HTMLButtonElement>(`[data-star="${star}"]`)
      ?.focus();
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLButtonElement>, star: number) => {
    const next =
      event.key === 'ArrowRight' || event.key === 'ArrowUp'
        ? Math.min(5, star + 1)
        : event.key === 'ArrowLeft' || event.key === 'ArrowDown'
          ? Math.max(1, star - 1)
          : event.key === 'Home'
            ? 1
            : event.key === 'End'
              ? 5
              : null;

    if (next === null) {
      return;
    }

    event.preventDefault();

    // En los extremos no se repite el valor: algunos usos lo interpretan como
    // «quitar puntuación» al volver a elegir la misma estrella.
    if (next !== star || Math.round(value) !== star) {
      onChange(next);
    }
    focusStar(next);
  };

  // Sin valor (0) la primera estrella es la que recibe el foco.
  const tabStop = value >= 1 ? Math.round(value) : 1;

  return (
    <div
      ref={groupRef}
      className="flex items-center gap-0.5"
      role="radiogroup"
      aria-label={
        label
          ? `Puntuación ${label}, de 1 a 5 estrellas`
          : 'Puntuación de 1 a 5 estrellas'
      }
    >
      {STARS.map((star) => (
        <button
          key={star}
          type="button"
          role="radio"
          data-star={star}
          aria-checked={star === Math.round(value)}
          aria-label={`${star} ${star === 1 ? 'estrella' : 'estrellas'}`}
          tabIndex={star === tabStop ? 0 : -1}
          onClick={() => onChange(star)}
          onKeyDown={(event) => handleKeyDown(event, star)}
          className={cn(
            'rounded-full transition hover:scale-110 focus:outline-none focus-visible:ring-2 focus-visible:ring-caramel-500',
            size === 'sm' ? 'p-1' : 'p-1.5',
          )}
        >
          <Star
            className={cn(
              iconClass,
              star <= Math.round(value)
                ? 'fill-caramel-500 text-caramel-500'
                : 'text-coffee-300',
            )}
            aria-hidden="true"
          />
        </button>
      ))}
    </div>
  );
}
