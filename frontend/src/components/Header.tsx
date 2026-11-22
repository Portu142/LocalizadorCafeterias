import { Coffee } from 'lucide-react';

export function Header() {
  return (
    <header className="sticky top-0 z-40 border-b border-coffee-100/80 bg-cream-50/85 backdrop-blur-xl">
      <div className="mx-auto flex max-w-[1600px] items-center justify-between gap-4 px-4 py-3 lg:px-8">
        <a href="#inicio" className="flex items-center gap-3 rounded-2xl" aria-label="Localizador de Cafés, volver al inicio">
          <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-coffee-900 text-cream-50 shadow-lg shadow-coffee-900/20">
            <Coffee className="h-5 w-5" aria-hidden="true" />
          </span>
          <span className="leading-tight">
            <span className="block font-display text-base font-semibold tracking-tight text-coffee-950 sm:text-lg">
              Localizador de Cafés
            </span>
            <span className="hidden text-[11px] font-medium uppercase tracking-[0.18em] text-coffee-600 sm:block">
              Cafeterías cerca de ti
            </span>
          </span>
        </a>

        <nav
          className="hidden items-center gap-6 text-sm font-medium text-coffee-700 md:flex"
          aria-label="Navegación principal"
        >
          <a
            className="inline-flex items-center py-1.5 transition hover:text-coffee-950"
            href="#explorar"
          >
            Explorar
          </a>
        </nav>

        <a
          className="inline-flex items-center rounded-full border border-coffee-200 bg-white/70 px-3 py-1.5 text-xs font-semibold text-coffee-800 transition hover:border-coffee-300 hover:bg-white md:hidden"
          href="#explorar"
        >
          Explorar
        </a>
      </div>
    </header>
  );
}
