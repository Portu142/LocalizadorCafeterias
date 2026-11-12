import { Heart } from 'lucide-react';
import { GITHUB_URL } from '../config';
import { GithubIcon } from './GithubIcon';

export function Footer() {
  return (
    <footer className="border-t border-coffee-100 bg-coffee-950 text-cream-100">
      <div className="mx-auto flex max-w-[1600px] flex-col items-center justify-between gap-4 px-4 py-8 text-center sm:flex-row sm:text-left lg:px-8">
        <div className="min-w-0">
          <p className="font-display text-lg font-semibold">Localizador de Cafés</p>
          <p className="mt-1 text-xs text-cream-100/70">
            Proyecto de portafolio · .NET 8 · PostgreSQL/PostGIS · React
          </p>
        </div>

        <div className="flex flex-wrap items-center justify-center gap-x-4 gap-y-2 text-xs text-cream-100/80 sm:justify-end">
          <a
            href={GITHUB_URL}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1.5 transition hover:text-cream-50"
          >
            <GithubIcon className="h-4 w-4" /> Ver código
          </a>
          <span className="inline-flex items-center gap-1.5">
            Hecho con{' '}
            <Heart className="h-3.5 w-3.5 fill-caramel-400 text-caramel-400" /> y
            mucho café
          </span>
        </div>
      </div>
    </footer>
  );
}
