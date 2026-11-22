import { Clock } from 'lucide-react';
import { CAFE_TAGS } from '../lib/cafeTags';
import { cn } from '../lib/utils';
import type { CafeTag } from '../types/cafe';

/** «abierto» + cualquier etiqueta de servicio. Se combinan entre sí (Y). */
export type ServiceFilter = 'abierto' | CafeTag;

type ServiceFiltersProps = {
  active: ReadonlySet<ServiceFilter>;
  onToggle: (filter: ServiceFilter) => void;
  onClear: () => void;
};

const TAG_FILTERS = Object.keys(CAFE_TAGS) as CafeTag[];

export function ServiceFilters({ active, onToggle, onClear }: ServiceFiltersProps) {
  const chip = (selected: boolean) =>
    cn(
      'inline-flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-full border px-3 py-2 text-xs font-semibold transition',
      selected
        ? 'border-caramel-500 bg-caramel-500/10 text-caramel-700'
        : 'border-coffee-200 bg-white/70 text-coffee-700 hover:border-coffee-300 hover:bg-white',
    );

  return (
    <div
      // En móvil, tira deslizable en horizontal; desde sm, filas que se ajustan.
      className="-mx-4 mb-4 flex items-center gap-2 overflow-x-auto px-4 pb-1 [scrollbar-width:none] sm:mx-0 sm:flex-wrap sm:overflow-visible sm:px-0 sm:pb-0"
      role="group"
      aria-label="Filtrar por horario y servicios"
    >
      <span className="mr-1 shrink-0 text-xs font-medium text-coffee-600">Filtrar:</span>

      <button
        type="button"
        aria-pressed={active.has('abierto')}
        onClick={() => onToggle('abierto')}
        className={chip(active.has('abierto'))}
      >
        <Clock className="h-3.5 w-3.5" aria-hidden="true" />
        Abierto ahora
      </button>

      {TAG_FILTERS.map((tag) => {
        const { label, icon: Icon } = CAFE_TAGS[tag];

        return (
          <button
            key={tag}
            type="button"
            aria-pressed={active.has(tag)}
            onClick={() => onToggle(tag)}
            className={chip(active.has(tag))}
          >
            <Icon className="h-3.5 w-3.5" aria-hidden="true" />
            {label}
          </button>
        );
      })}

      {active.size > 0 && (
        <button
          type="button"
          onClick={onClear}
          className="shrink-0 whitespace-nowrap px-1 py-2 text-xs font-semibold text-caramel-600 underline-offset-2 hover:underline"
        >
          Quitar filtros ({active.size})
        </button>
      )}
    </div>
  );
}
