import { lazy, Suspense, useMemo, useRef, useState } from 'react';
import { CafeDetail } from './components/CafeDetail';
import { CafeList } from './components/CafeList';
import { Footer } from './components/Footer';
import { Header } from './components/Header';
import { Hero } from './components/Hero';
import { LocationFilter } from './components/LocationFilter';
import { LocationBanner } from './components/LocationBanner';
import { NeighborhoodFilter } from './components/NeighborhoodFilter';
import { IS_DEMO_MODE } from './config';
import { useCafe } from './hooks/useCafe';
import { useCafes } from './hooks/useCafes';
import { useFavorites } from './hooks/useFavorites';
import { useGeolocation } from './hooks/useGeolocation';
import { DEFAULT_LOCATION, distanceKm } from './lib/geo';

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
  // Pide la ubicación nada más cargar: si el usuario la concede, la app carga
  // automáticamente los cafés reales de su zona. Si la deniega, se mantiene la
  // ubicación de referencia con un aviso claro y el botón para reintentar.
  const geo = useGeolocation({ autoRequest: true });
  const isLocating = geo.status === 'requesting';
  const locationActive = geo.status === 'granted' && geo.coordinates !== null;
  const userCoordinates = locationActive ? geo.coordinates : null;

  // Mientras se resuelve el permiso no se consulta la ubicación de referencia:
  // evita un parpadeo de cafeterías de Madrid antes de las de tu zona.
  const { data: cafes = [], isLoading, isError, refetch } = useCafes(
    userCoordinates ?? DEFAULT_LOCATION,
    { enabled: !isLocating },
  );
  const { favorites, favoriteIds, toggleFavorite, isFavorite } = useFavorites();

  const [search, setSearch] = useState('');
  const [neighborhood, setNeighborhood] = useState(ALL_NEIGHBORHOODS);
  const [showFavorites, setShowFavorites] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const detailRef = useRef<HTMLDivElement>(null);

  const neighborhoods = useMemo(() => {
    const unique = Array.from(
      new Set(cafes.map((cafe) => cafe.neighborhood).filter(Boolean)),
    ).sort((a, b) => a.localeCompare(b, 'es'));

    return [ALL_NEIGHBORHOODS, ...unique];
  }, [cafes]);

  const distances = useMemo(() => {
    const map = new Map<string, number>();

    // Las distancias se calculan respecto a la ubicación REAL del usuario
    // (no a la predeterminada), para que el punto azul del mapa y la ficha
    // muestren la distancia correcta cuando se active "Cerca de mí".
    const reference = userCoordinates ?? DEFAULT_LOCATION;

    for (const cafe of cafes) {
      map.set(
        cafe.id,
        distanceKm(reference, {
          latitude: cafe.latitude,
          longitude: cafe.longitude,
        }),
      );
    }

    return map;
  }, [cafes, userCoordinates]);

  // Favoritos con datos frescos cuando el café está en la lista actual; si no
  // (porque estás en otra zona), se usa la ficha guardada en localStorage.
  const favoriteCafes = useMemo(() => {
    const byId = new Map(cafes.map((cafe) => [cafe.id, cafe]));

    return favorites.map((favorite) => byId.get(favorite.id) ?? favorite);
  }, [cafes, favorites]);

  const filteredCafes = useMemo(() => {
    const term = normalize(search);

    // En favoritos se parte de las fichas guardadas, no de los cafés de la
    // ubicación actual: así la lista no cambia al moverte de zona. Si el café
    // también está en los resultados actuales se usa su versión más reciente.
    const base = showFavorites ? favoriteCafes : cafes;

    const matches = base.filter((cafe) => {
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

    // La vista de favoritos es independiente de la ubicación: nunca se filtra
    // por radio ni se ordena por cercanía.
    if (showFavorites) {
      return matches;
    }

    // Con ubicación activa, se filtra por radio y ordena por cercanía.
    if (userCoordinates) {
      return matches
        .filter((cafe) => (distances.get(cafe.id) ?? Number.POSITIVE_INFINITY) <= RADIUS_KM)
        .sort(
          (a, b) => (distances.get(a.id) ?? 0) - (distances.get(b.id) ?? 0),
        );
    }

    // Sin ubicación del usuario, se muestra la lista completa (ya cargada
    // desde la ubicación predeterminada) sin radio de cercanía.
    return matches;
  }, [cafes, distances, favoriteCafes, neighborhood, search, showFavorites, userCoordinates]);

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
      neighborhoods: new Set(
        cafes.map((cafe) => cafe.neighborhood).filter(Boolean),
      ).size,
      reviews: cafes.reduce((total, cafe) => total + cafe.reviewCount, 0),
    }),
    [cafes],
  );

  // Indica de dónde salen los datos que se muestran en el listado.
  const sourceNote = !IS_DEMO_MODE
    ? 'Datos servidos por la API (.NET + PostgreSQL)'
    : locationActive
      ? 'Cafés reales de OpenStreetMap cerca de tu ubicación'
      : 'Cafés reales de OpenStreetMap en Madrid (referencia)';

  const locationNotice = isLocating
    ? 'Pidiendo permiso para acceder a tu ubicación…'
    : locationActive
      ? isLoading
        ? 'Buscando cafeterías reales cerca de ti… puede tardar unos segundos.'
        : isError
          ? 'No se pudieron cargar las cafeterías cercanas.'
          : `Mostrando las cafeterías a menos de ${RADIUS_KM} km, ordenadas por cercanía.`
      : geo.status === 'denied' ||
          geo.status === 'blocked' ||
          geo.status === 'unavailable' ||
          geo.status === 'error'
        ? `${geo.message} Mientras tanto, mostramos cafeterías de Madrid como referencia.`
        : 'Mostrando cafeterías de Madrid como referencia. Pulsa «Usar mi ubicación» para ver las de tu zona.';

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

        {!locationActive && !isLocating && (
          <LocationBanner status={geo.status} onRequest={geo.request} />
        )}

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
              favorites={favoriteIds}
              distances={distances}
              isLoading={isLoading || isLocating}
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
              sourceNote={sourceNote}
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
                isLoading={isLocating || isLoading}
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
                if (selectedCafe) {
                  toggleFavorite(selectedCafe);
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
