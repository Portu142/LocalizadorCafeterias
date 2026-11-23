import { Coffee } from 'lucide-react';
import { AccountMenu } from './AccountMenu';

type HeaderProps = {
  onLogin: () => void;
  onMyReviews: () => void;
};

export function Header({ onLogin, onMyReviews }: HeaderProps) {
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

        {/* Un único «Explorar», junto al botón de cuenta en todos los tamaños
            (antes, en escritorio, quedaba suelto en el centro de la cabecera). */}
        <nav className="flex items-center gap-2" aria-label="Navegación principal">
          <a
            className="inline-flex items-center rounded-full border border-coffee-200 bg-white/70 px-3.5 py-2 text-xs font-semibold text-coffee-800 transition hover:border-coffee-300 hover:bg-white"
            href="#explorar"
          >
            Explorar
          </a>
          <AccountMenu onLogin={onLogin} onMyReviews={onMyReviews} />
        </nav>
      </div>
    </header>
  );
}
