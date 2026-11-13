import { Coffee } from 'lucide-react';
import { IS_DEMO_MODE } from '../config';

const YEAR = new Date().getFullYear();

export function Footer() {
  return (
    <footer className="border-t border-coffee-100 bg-cream-50">
      <div className="mx-auto flex max-w-[1600px] flex-col gap-6 px-4 py-8 sm:flex-row sm:items-center sm:justify-between lg:px-8">
        <div className="flex items-center gap-2.5">
          <span className="flex h-8 w-8 items-center justify-center rounded-xl bg-coffee-900 text-cream-50">
            <Coffee className="h-4 w-4" aria-hidden="true" />
          </span>
          <div>
            <p className="font-display text-sm font-semibold text-coffee-950">
              Localizador de Cafés
            </p>
            <p className="text-[11px] text-coffee-500">
              Cafeterías de especialidad en Madrid
            </p>
          </div>
        </div>

        <nav
          className="flex flex-wrap items-center gap-x-5 gap-y-2 text-xs font-medium text-coffee-600"
          aria-label="Enlaces del pie de página"
        >
          <a href="#explorar" className="transition hover:text-coffee-950">
            Explorar
          </a>
          <a href="#inicio" className="transition hover:text-coffee-950">
            Volver arriba
          </a>
          <a
            href="https://www.openstreetmap.org/copyright"
            target="_blank"
            rel="noopener noreferrer"
            className="transition hover:text-coffee-950"
          >
            Mapa © OpenStreetMap
          </a>
        </nav>
      </div>

      <div className="border-t border-coffee-100/80">
        <div className="mx-auto flex max-w-[1600px] flex-col gap-1.5 px-4 py-4 text-[11px] text-coffee-500 sm:flex-row sm:items-center sm:justify-between lg:px-8">
          <p>© {YEAR} Localizador de Cafés</p>
          {IS_DEMO_MODE && (
            <p>Datos de demostración · Las reseñas se guardan en tu navegador</p>
          )}
        </div>
      </div>
    </footer>
  );
}
