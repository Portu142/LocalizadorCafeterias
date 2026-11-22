import { lazy, Suspense, useMemo, useRef, useState } from 'react';
import { AuthDialog, type AuthMode } from './components/AuthDialog';
import { CafeDetail } from './components/CafeDetail';
import { MyReviewsDialog } from './components/MyReviewsDialog';
import { CafeList } from './components/CafeList';
import { DemoNotice } from './components/DemoNotice';
import { Footer } from './components/Footer';
import { Header } from './components/Header';
import { Hero } from './components/Hero';
import { LocationFilter } from './components/LocationFilter';
import { LocationBanner } from './components/LocationBanner';
import { NeighborhoodFilter } from './components/NeighborhoodFilter';
import { ServiceFilters, type ServiceFilter } from './components/ServiceFilters';
import { IS_DEMO_MODE } from './config';
import { useCafe } from './hooks/useCafe';
import { useCafes } from './hooks/useCafes';
import { useFavorites } from './hooks/useFavorites';
import { useGeolocation } from './hooks/useGeolocation';
import { DEFAULT_LOCATION, distanceKm } from './lib/geo';
import { isOpenAt } from './lib/openingHours';

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
  const {
    data: cafeSearch,
    isLoading,
    isError,
    refetch,
  } = useCafes(userCoordinates ?? DEFAULT_LOCATION, { enabled: !isLocating });
  const cafes = useMemo(() => cafeSearch?.cafes ?? [], [cafeSearch]);
  const dataSource = cafeSearch?.source ?? null;
  const { favorites, favoriteIds, toggleFavorite, isFavorite } = useFavorites();

  const [search, setSearch] = useState('');
  const [neighborhood, setNeighborhood] = useState(ALL_NEIGHBORHOODS);
  const [showFavorites, setShowFavorites] = useState(false);
  const [serviceFilters, setServiceFilters] = useState<ReadonlySet<ServiceFilter>>(
    () => new Set(),
  );
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [authMode, setAuthMode] = useState<AuthMode | null>(null);
  const [myReviewsOpen, setMyReviewsOpen] = useState(false);
  const detailRef = useRef<HTMLDivElement>(null);

  const neighborhoods = useMemo(() => {
    const unique = Array.from(
      new Set(cafes.map((cafe) => cafe.neighborhood).filter(Boolean)),
    ).sort((a, b) => a.localeCompare(b, 'es'));

    return [ALL_NEIGHBORHOODS, ...unique];
  }, [cafes]);

  const distances = useMemo(() => {
    const map = new Map<string, number>();

    // Solo hay distancias con la ubicación REAL del usuario: medirlas desde la
    // ubicación de referencia (Sol) mostraba «Estás a 261 m de aquí» a alguien
    // que no está en Madrid.
    if (!userCoordinates) {
      return map;
    }

    for (const cafe of cafes) {
      map.set(
        cafe.id,
        distanceKm(userCoordinates, {
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

      // Filtros combinables: el café debe cumplirlos todos. Sin dato (horario
      // desconocido, servicio no registrado) se considera que no lo cumple.
      for (const filter of serviceFilters) {
        if (filter === 'abierto' ? isOpenAt(cafe.hours) !== true : !cafe.tags?.includes(filter)) {
          return false;
        }
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
  }, [
    cafes,
    distances,
    favoriteCafes,
    neighborhood,
    search,
    serviceFilters,
    showFavorites,
    userCoordinates,
  ]);

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

  // Indica de dónde salen los datos que se muestran en el listado. Con la API
  // activa puede ser PostgreSQL (Madrid) o el fallback de OpenStreetMap cuando
  // no hay cafeterías cerca de la ubicación real.
  const sourceNote =
    dataSource === 'osm'
      ? IS_DEMO_MODE
        ? locationActive
          ? 'Cafés reales de OpenStreetMap cerca de tu ubicación (sin carta)'
          : 'Cafés reales de OpenStreetMap en Madrid (sin carta)'
        : 'Cafés reales de OpenStreetMap cerca de ti (la API solo cubre Madrid)'
      : 'Datos servidos por la API (.NET + PostgreSQL)';

  const locationNotice = isLocating
    ? 'Pidiendo permiso para acceder a tu ubicación…'
    : locationActive
      ? isLoading
        ? 'Buscando cafeterías reales cerca de ti… puede tardar unos segundos.'
        : isError
          ? 'No se pudieron cargar las cafeterías cercanas.'
          : `Mostrando las cafeterías a menos de ${RADIUS_KM} km, ordenadas por cercanía.`
      : // El motivo concreto (permiso denegado, sin señal…) ya lo explica
        // LocationBanner justo encima: aquí no se repite.
        'Mostrando cafeterías de Madrid como referencia.';

  const locationCausedEmpty =
    locationActive &&
    !showFavorites &&
    !search.trim() &&
    serviceFilters.size === 0 &&
    neighborhood === ALL_NEIGHBORHOODS;

  const toggleServiceFilter = (filter: ServiceFilter) =>
    setServiceFilters((current) => {
      const next = new Set(current);
      if (next.has(filter)) {
        next.delete(filter);
      } else {
        next.add(filter);
      }
      return next;
    });

  // Solo se desactiva la ubicación cuando es ella la que deja la lista vacía:
  // borrar una búsqueda no debe hacerte perder «Cerca de mí».
  const resetFilters = () => {
    setSearch('');
    setNeighborhood(ALL_NEIGHBORHOODS);
    setShowFavorites(false);
    setServiceFilters(new Set());

    if (locationCausedEmpty) {
      geo.clear();
    }
  };

  const emptyActionLabel = showFavorites
    ? 'Ver todas las cafeterías'
    : locationCausedEmpty
      ? 'Quitar «Cerca de mí»'
      : 'Limpiar filtros';

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

      <Header
        onLogin={() => setAuthMode('login')}
        onMyReviews={() => setMyReviewsOpen(true)}
      />
      <Hero
        search={search}
        onSearchChange={setSearch}
        stats={stats}
        isLoading={isLoading || isLocating}
      />

      <main id="explorar" className="mx-auto max-w-[1600px] px-4 py-8 lg:px-8">
        {IS_DEMO_MODE && <DemoNotice />}

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

        <ServiceFilters
          active={serviceFilters}
          onToggle={toggleServiceFilter}
          onClear={() => setServiceFilters(new Set())}
        />

        {!locationActive && !isLocating && (
          <LocationBanner
            status={geo.status}
            message={geo.message}
            onRequest={geo.request}
          />
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
          {/* Altura fija también en móvil: con 60 resultados la lista ocupaba
              miles de píxeles y el mapa y la ficha quedaban fuera de alcance. */}
          <div className="h-[460px] sm:h-[520px] lg:h-[620px] xl:h-[720px]">
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
                    : serviceFilters.size > 0
                      ? 'Solo aparecen las cafeterías que tienen ese horario o servicio registrado. Prueba a quitar algún filtro.'
                      : locationActive
                        ? 'Prueba con otro barrio, borra la búsqueda o quita “Cerca de mí”.'
                        : 'Prueba con otro barrio o borra la búsqueda.'
              }
              onSelect={handleSelect}
              onToggleFavorite={toggleFavorite}
              onRetry={() => void refetch()}
              onClearFilters={resetFilters}
              emptyActionLabel={emptyActionLabel}
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
                  Cargando mapa…
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
              isLoading={isDetailLoading || (isLoading && !selectedCafe)}
              isError={isDetailError}
              onRetry={() => void refetchDetail()}
              isFavorite={effectiveSelectedId ? isFavorite(effectiveSelectedId) : false}
              onToggleFavorite={() => {
                if (selectedCafe) {
                  toggleFavorite(selectedCafe);
                }
              }}
              onRequestLogin={() => setAuthMode('login')}
            />
          </div>
        </div>
      </main>

      <Footer />

      <AuthDialog
        mode={authMode}
        onModeChange={setAuthMode}
        onClose={() => setAuthMode(null)}
      />
      <MyReviewsDialog open={myReviewsOpen} onClose={() => setMyReviewsOpen(false)} />
    </div>
  );
}

export default App;
