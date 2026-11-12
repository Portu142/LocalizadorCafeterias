import { lazy, Suspense, useMemo, useRef, useState } from 'react';
import { CafeDetail } from './components/CafeDetail';
import { CafeList } from './components/CafeList';
import { Footer } from './components/Footer';
import { Header } from './components/Header';
import { Hero } from './components/Hero';
import { NeighborhoodFilter } from './components/NeighborhoodFilter';
import { TechSection } from './components/TechSection';
import { useCafe } from './hooks/useCafe';
import { useCafes } from './hooks/useCafes';
import { useFavorites } from './hooks/useFavorites';

const MapView = lazy(() =>
  import('./components/MapView').then((module) => ({ default: module.MapView })),
);

const ALL_NEIGHBORHOODS = 'Todas';

function normalize(value: string) {
  return value
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .trim();
}

function App() {
  const { data: cafes = [], isLoading, isError, refetch } = useCafes();
  const { favorites, toggleFavorite, isFavorite } = useFavorites();

  const [search, setSearch] = useState('');
  const [neighborhood, setNeighborhood] = useState(ALL_NEIGHBORHOODS);
  const [showFavorites, setShowFavorites] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const detailRef = useRef<HTMLDivElement>(null);

  const neighborhoods = useMemo(() => {
    const unique = Array.from(new Set(cafes.map((cafe) => cafe.neighborhood))).sort(
      (a, b) => a.localeCompare(b, 'es'),
    );

    return [ALL_NEIGHBORHOODS, ...unique];
  }, [cafes]);

  const filteredCafes = useMemo(() => {
    const term = normalize(search);

    return cafes.filter((cafe) => {
      if (showFavorites && !favorites.includes(cafe.id)) {
        return false;
      }

      if (neighborhood !== ALL_NEIGHBORHOODS && cafe.neighborhood !== neighborhood) {
        return false;
      }

      if (!term) {
        return true;
      }

      const haystack = normalize(
        `${cafe.name} ${cafe.address} ${cafe.neighborhood}`,
      );

      return haystack.includes(term);
    });
  }, [cafes, favorites, neighborhood, search, showFavorites]);

  const effectiveSelectedId = useMemo(() => {
    if (selectedId && filteredCafes.some((cafe) => cafe.id === selectedId)) {
      return selectedId;
    }

    return filteredCafes[0]?.id ?? null;
  }, [filteredCafes, selectedId]);

  const {
    data: selectedCafe,
    isLoading: isDetailLoading,
    isError: isDetailError,
    refetch: refetchDetail,
  } = useCafe(effectiveSelectedId);

  const stats = useMemo(
    () => ({
      cafes: cafes.length,
      neighborhoods: new Set(cafes.map((cafe) => cafe.neighborhood)).size,
      reviews: cafes.reduce((total, cafe) => total + cafe.reviewCount, 0),
    }),
    [cafes],
  );

  const resetFilters = () => {
    setSearch('');
    setNeighborhood(ALL_NEIGHBORHOODS);
    setShowFavorites(false);
  };

  const handleSelect = (id: string) => {
    setSelectedId(id);

    if (window.matchMedia('(max-width: 1023px)').matches) {
      const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

      requestAnimationFrame(() => {
        detailRef.current?.scrollIntoView({
          behavior: reduceMotion ? 'auto' : 'smooth',
          block: 'start',
        });
      });
    }
  };

  return (
    <div className="min-h-screen bg-cream-100">
      <a
        href="#explorar"
        className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-50 focus:rounded-full focus:bg-coffee-900 focus:px-4 focus:py-2 focus:text-sm focus:font-semibold focus:text-cream-50"
      >
        Saltar al contenido principal
      </a>

      <Header />
      <Hero search={search} onSearchChange={setSearch} stats={stats} />

      <main id="explorar" className="mx-auto max-w-[1600px] px-4 py-8 lg:px-8">
        <div className="mb-5 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <h2 className="font-display text-2xl font-semibold tracking-tight text-coffee-950 sm:text-3xl">
              Explora las cafeterías
            </h2>
            <p className="mt-1 text-sm text-coffee-600">
              Filtra por barrio, guarda tus favoritos y descubre cada rincón.
            </p>
          </div>

          <NeighborhoodFilter
            neighborhoods={neighborhoods}
            active={neighborhood}
            onChange={setNeighborhood}
            showFavorites={showFavorites}
            onToggleFavorites={() => setShowFavorites((value) => !value)}
            favoritesCount={favorites.length}
          />
        </div>

        {search && (
          <p className="mb-4 text-xs text-coffee-600" role="status">
            Resultados para <span className="font-semibold text-coffee-800">“{search}”</span>
            {' · '}
            <button
              type="button"
              onClick={() => setSearch('')}
              className="font-semibold text-caramel-600 underline-offset-2 hover:underline"
            >
              borrar búsqueda
            </button>
          </p>
        )}

        <div className="grid gap-4 lg:grid-cols-2 xl:grid-cols-[minmax(0,340px)_minmax(0,1fr)_minmax(0,420px)]">
          <div className="h-auto lg:h-[620px] xl:h-[720px]">
            <CafeList
              cafes={filteredCafes}
              selectedId={effectiveSelectedId}
              favorites={favorites}
              isLoading={isLoading}
              isError={isError}
              emptyTitle={
                showFavorites
                  ? 'Todavía no tienes favoritos.'
                  : 'No hay resultados con esos filtros.'
              }
              emptyDescription={
                showFavorites
                  ? 'Pulsa el corazón de una cafetería para guardarla aquí.'
                  : 'Prueba con otro barrio o borra la búsqueda.'
              }
              onSelect={handleSelect}
              onToggleFavorite={toggleFavorite}
              onRetry={() => void refetch()}
              onClearFilters={resetFilters}
            />
          </div>

          <div className="h-[360px] lg:h-[620px] xl:h-[720px]">
            <Suspense
              fallback={
                <div
                  className="flex h-full items-center justify-center rounded-3xl border border-coffee-100 bg-cream-50 text-sm text-coffee-500"
                  role="status"
                >
                  Cargando mapa...
                </div>
              }
            >
              <MapView
                cafes={filteredCafes}
                selectedId={effectiveSelectedId}
                onSelect={handleSelect}
              />
            </Suspense>
          </div>

          <div
            ref={detailRef}
            id="ficha"
            className="h-auto scroll-mt-24 lg:col-span-2 xl:col-span-1 xl:h-[720px]"
          >
            <CafeDetail
              cafe={selectedCafe}
              isLoading={isDetailLoading}
              isError={isDetailError}
              onRetry={() => void refetchDetail()}
              isFavorite={effectiveSelectedId ? isFavorite(effectiveSelectedId) : false}
              onToggleFavorite={() => {
                if (effectiveSelectedId) {
                  toggleFavorite(effectiveSelectedId);
                }
              }}
            />
          </div>
        </div>
      </main>

      <TechSection />
      <Footer />
    </div>
  );
}

export default App;
