import { Coffee, MapPin, Search, Star } from 'lucide-react';

type HeroProps = {
  search: string;
  onSearchChange: (value: string) => void;
  stats: {
    cafes: number;
    neighborhoods: number;
    reviews: number;
  };
};

export function Hero({ search, onSearchChange, stats }: HeroProps) {
  return (
    <section
      id="inicio"
      className="relative overflow-hidden border-b border-coffee-100 bg-gradient-to-b from-cream-50 to-cream-100"
    >
      <div className="pointer-events-none absolute -right-24 -top-24 h-80 w-80 rounded-full bg-caramel-400/10 blur-3xl" />
      <div className="pointer-events-none absolute -left-20 bottom-0 h-72 w-72 rounded-full bg-coffee-300/20 blur-3xl" />

      <div className="relative mx-auto max-w-[1600px] px-4 py-14 lg:px-8 lg:py-20">
        <div className="max-w-3xl">
          <span className="inline-flex items-center gap-2 rounded-full border border-coffee-200 bg-white/70 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-coffee-700">
            <Coffee className="h-3.5 w-3.5" aria-hidden="true" />
            Hecho para amantes del café
          </span>

          <h1 className="mt-6 font-display text-4xl font-semibold leading-[1.05] tracking-tight text-coffee-950 sm:text-5xl lg:text-6xl">
            Tu próximo café,
            <br />
            <span className="text-caramel-600">a un clic.</span>
          </h1>

          <p className="mt-5 max-w-2xl text-base leading-7 text-coffee-700 sm:text-lg">
            Descubre cafeterías de especialidad cerca de ti, consulta horarios,
            menús y reseñas. Concede tu ubicación y ordena los resultados por
            cercanía para encontrar el sitio perfecto.
          </p>

          <form
            role="search"
            onSubmit={(event) => event.preventDefault()}
            className="mt-8 flex max-w-2xl items-center gap-2 rounded-2xl border border-coffee-200 bg-white p-2 shadow-soft"
          >
            <label htmlFor="cafe-search" className="sr-only">
              Buscar cafeterías por nombre, barrio o dirección
            </label>
            <Search
              className="ml-2 h-5 w-5 shrink-0 text-coffee-500"
              aria-hidden="true"
            />
            <input
              id="cafe-search"
              type="search"
              value={search}
              onChange={(event) => onSearchChange(event.target.value)}
              placeholder="Busca por nombre, barrio o dirección..."
              className="min-w-0 flex-1 bg-transparent px-1 py-2 text-sm text-coffee-950 outline-none placeholder:text-coffee-500 sm:text-base"
            />
            {search && (
              <button
                type="button"
                onClick={() => onSearchChange('')}
                className="rounded-xl px-3 py-2 text-xs font-semibold text-coffee-600 transition hover:bg-cream-100 hover:text-coffee-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-caramel-500"
              >
                Limpiar
              </button>
            )}
          </form>

          <dl className="mt-10 grid max-w-xl grid-cols-3 gap-4">
            <div className="rounded-2xl border border-coffee-100 bg-white/70 p-4">
              <dt className="flex items-center gap-1.5 text-xs font-medium text-coffee-600">
                <Coffee className="h-3.5 w-3.5" aria-hidden="true" /> Cafeterías
              </dt>
              <dd className="mt-1 font-display text-2xl font-semibold text-coffee-950">
                {stats.cafes}
              </dd>
            </div>
            <div className="rounded-2xl border border-coffee-100 bg-white/70 p-4">
              <dt className="flex items-center gap-1.5 text-xs font-medium text-coffee-600">
                <MapPin className="h-3.5 w-3.5" aria-hidden="true" /> Barrios
              </dt>
              <dd className="mt-1 font-display text-2xl font-semibold text-coffee-950">
                {stats.neighborhoods}
              </dd>
            </div>
            <div className="rounded-2xl border border-coffee-100 bg-white/70 p-4">
              <dt className="flex items-center gap-1.5 text-xs font-medium text-coffee-600">
                <Star className="h-3.5 w-3.5" aria-hidden="true" /> Reseñas
              </dt>
              <dd className="mt-1 font-display text-2xl font-semibold text-coffee-950">
                {stats.reviews}
              </dd>
            </div>
          </dl>
        </div>
      </div>
    </section>
  );
}
