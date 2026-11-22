import { MapPin, Sparkles } from 'lucide-react';
import type { DemoSource } from '../api/cafes';
import { cn } from '../lib/utils';

type DemoNoticeProps = {
  source: DemoSource;
  onChange: (source: DemoSource) => void;
};

const OPTIONS: Array<{ value: DemoSource; label: string; hint: string }> = [
  {
    value: 'sample',
    label: 'Ejemplos con menú',
    hint: '6 cafeterías de Madrid con carta y reseñas',
  },
  {
    value: 'osm',
    label: 'Cafés reales (OSM)',
    hint: 'Datos abiertos de tu zona, sin carta',
  },
];

/**
 * Explica el modo demo y permite elegir entre los ejemplos curados (con menú)
 * y los cafés reales de OpenStreetMap (sin menú).
 */
export function DemoNotice({ source, onChange }: DemoNoticeProps) {
  return (
    <div className="mb-5 flex flex-col gap-3 rounded-2xl border border-coffee-200 bg-cream-50 px-4 py-3 lg:flex-row lg:items-center lg:justify-between">
      <div className="flex items-start gap-2.5">
        <span className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-caramel-500/15 text-caramel-700">
          <Sparkles className="h-4 w-4" aria-hidden="true" />
        </span>
        <div className="text-sm">
          <p className="font-semibold text-coffee-900">Estás en el modo demo</p>
          <p className="mt-0.5 text-xs leading-5 text-coffee-600">
            Esta versión estática no usa la API .NET: las reseñas se guardan en
            tu navegador. Los ejemplos incluyen menú; OpenStreetMap no publica
            cartas, así que esos cafés muestran el menú vacío.
          </p>
        </div>
      </div>

      <div
        className="flex shrink-0 flex-wrap gap-1 rounded-full bg-cream-100 p-1"
        role="group"
        aria-label="Fuente de datos de la demo"
      >
        {OPTIONS.map((option) => {
          const active = source === option.value;

          return (
            <button
              key={option.value}
              type="button"
              onClick={() => onChange(option.value)}
              aria-pressed={active}
              title={option.hint}
              className={cn(
                'inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-semibold transition focus:outline-none focus-visible:ring-2 focus-visible:ring-caramel-500',
                active
                  ? 'bg-coffee-900 text-cream-50 shadow-sm'
                  : 'text-coffee-600 hover:text-coffee-950',
              )}
            >
              {option.value === 'osm' && (
                <MapPin className="h-3.5 w-3.5" aria-hidden="true" />
              )}
              {option.label}
            </button>
          );
        })}
      </div>
    </div>
  );
}
