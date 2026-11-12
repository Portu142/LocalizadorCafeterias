import { Coffee, Sparkles } from 'lucide-react';
import { GITHUB_URL, IS_DEMO_MODE } from '../config';
import { GithubIcon } from './GithubIcon';

export function Header() {
  return (
    <header className="sticky top-0 z-40 border-b border-coffee-100/80 bg-cream-50/85 backdrop-blur-xl">
      <div className="mx-auto flex max-w-[1600px] items-center justify-between gap-4 px-4 py-3 lg:px-8">
        <a href="#inicio" className="flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-coffee-900 text-cream-50 shadow-lg shadow-coffee-900/20">
            <Coffee className="h-5 w-5" />
          </span>
          <span className="leading-tight">
            <span className="block font-display text-base font-semibold tracking-tight text-coffee-950 sm:text-lg">
              Localizador de Cafés
            </span>
            <span className="hidden text-[11px] font-medium uppercase tracking-[0.18em] text-coffee-600 sm:block">
              Café de especialidad · Madrid
            </span>
          </span>
        </a>

        <nav className="hidden items-center gap-6 text-sm font-medium text-coffee-700 md:flex">
          <a className="transition hover:text-coffee-950" href="#explorar">
            Explorar
          </a>
          <a className="transition hover:text-coffee-950" href="#proyecto">
            Proyecto
          </a>
          <a
            className="inline-flex items-center gap-2 rounded-full border border-coffee-200 bg-white/70 px-3 py-1.5 text-xs font-semibold text-coffee-800 transition hover:border-coffee-300 hover:bg-white"
            href={GITHUB_URL}
            target="_blank"
            rel="noopener noreferrer"
          >
            <GithubIcon className="h-4 w-4" />
            GitHub
          </a>
        </nav>

        <div className="flex items-center gap-2">
          <a
            className="inline-flex items-center justify-center rounded-full border border-coffee-200 bg-white/70 p-2 text-coffee-800 transition hover:border-coffee-300 hover:bg-white md:hidden"
            href={GITHUB_URL}
            target="_blank"
            rel="noopener noreferrer"
            aria-label="Ver el código en GitHub"
          >
            <GithubIcon className="h-4 w-4" />
          </a>

          {IS_DEMO_MODE && (
            <span className="inline-flex items-center gap-1.5 rounded-full bg-caramel-500/10 px-3 py-1 text-xs font-semibold text-caramel-700 ring-1 ring-inset ring-caramel-500/20">
              <Sparkles className="h-3.5 w-3.5" />
              Demo
            </span>
          )}
        </div>
      </div>
    </header>
  );
}
