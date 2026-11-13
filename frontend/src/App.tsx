import { lazy, Suspense, useMemo, useRef, useState } from 'react';
import { CafeDetail } from './components/CafeDetail';
import { CafeList } from './components/CafeList';
import { Footer } from './components/Footer';
import { Header } from './components/Header';
import { Hero } from './components/Hero';
import { LocationFilter } from './components/LocationFilter';
import { NeighborhoodFilter } from './components/NeighborhoodFilter';
import { useCafe } from './hooks/useCafe';
import { useCafes } from './hooks/useCafes';
import { useFavorites } from './hooks/useFavorites';
import { useGeolocation } from './hooks/useGeolocation';
import { distanceKm } from './lib/geo';

const MapView = lazy(() =>
  import('./components/MapView').then((module) => ({ default: module.MapView })),
);

const ALL_NEIGHBORHOODS = 'Todas';
const RADIUS_KM = 5;

function normalize(value: string) {
  return value
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .trim();
}

function App() {
  const geo = useGeolocation();
  const locationActive = geo.status === 'granted' && geo.coordinates !== null;
  const userCoordinates = locationActive ? geo.coordinates : null;

  const { data: cafes = [], isLoading, isError, refetch } = useCafes(userCoordinates);
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

  const distances = useMemo(() => {
    const map = new Map<string, number>();

    if (userCoordinates) {
      for (const cafe of cafes) {
        map.set(
          cafe.id,
          distanceKm(userCoordinates, {
            latitude: cafe.latitude,
            longitude: cafe.longitude,
          }),
        );
      }
    }

    return map;
  }, [cafes, userCoordinates]);

  const filteredCafes = useMemo(() => {
    const term = normalize(search);

    const matches = cafes.filter((cafe) => {
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

    if (!userCoordinates) {
      return matches;
    }

    return matches
      .filter((cafe) => (distances.get(cafe.id) ?? Number.POSITIVE_INFINITY) <= RADIUS_KM)
      .sort(
        (a, b) => (distances.get(a.id) ?? 0) - (distances.get(b.id) ?? 0),
      );
  }, [cafes, distances, favorites, neighborhood, search, showFavorites, userCoordinates]);

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

  const locationNotice =
    geo.status === 'requesting'
      ? 'Buscando tu ubicación…'
      : locationActive
        ? `Mostrando las cafeterías a menos de ${RADIUS_KM} km, ordenadas por cercanía.`
        : geo.message;

  const locationCausedEmpty =
    locationActive &&
    !showFavorites &&
    !search.trim() &&
    neighborhood === ALL_NEIGHBORHOODS;

  const resetFilters = () => {
    setSearch('');
    setNeighborhood(ALL_NEIGHBORHOODS);
    setShowFavorites(false);
    geo.clear();
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
              Filtra por barrio, activa tu ubicación para ver lo más cercano y
              guarda tus favoritos.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2 lg:justify-end">
            <LocationFilter
              status={geo.status}
              onRequest={geo.request}
              onClear={geo.clear}
            />
            <NeighborhoodFilter
              neighborhoods={neighborhoods}
              active={neighborhood}
              onChange={setNeighborhood}
              showFavorites={showFavorites}
              onToggleFavorites={() => setShowFavorites((value) => !value)}
              favoritesCount={favorites.length}
            />
          </div>
        </div>

        {locationNotice && (
          <p className="mb-4 text-xs text-coffee-600" role="status">
            {locationNotice}
          </p>
        )}

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
              distances={distances}
              isLoading={isLoading}
              isError={isError}
              emptyTitle={
                showFavorites
                  ? 'Todavía no tienes favoritos.'
                  : locationCausedEmpty
                    ? 'No hay cafeterías cerca de ti.'
                    : 'No hay resultados con esos filtros.'
              }
              emptyDescription={
                showFavorites
                  ? 'Pulsa el corazón de una cafetería para guardarla aquí.'
                  : locationCausedEmpty
                    ? `No hay ninguna a menos de ${RADIUS_KM} km. Desactiva “Cerca de mí” para ver todas.`
                    : locationActive
                      ? 'Prueba con otro barrio, borra la búsqueda o quita “Cerca de mí”.'
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
                selectedId={selectedId}
                userLocation={userCoordinates}
                emptyMessage={
                  locationCausedEmpty
                    ? 'No hay cafeterías cerca de tu ubicación.'
                    : 'No hay cafeterías en el mapa con estos filtros.'
                }
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
              distanceKm={
                effectiveSelectedId ? distances.get(effectiveSelectedId) : undefined
              }
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

      <Footer />
    </div>
  );
}

export default App;
