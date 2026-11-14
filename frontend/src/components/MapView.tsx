import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet.markercluster';
import { MapContainer, Marker, TileLayer, useMap } from 'react-leaflet';
import type { Coordinates } from '../lib/geo';
import type { CafeSummary } from '../types/cafe';

const MADRID_CENTER: [number, number] = [40.4168, -3.7038];

/** Máximo de cafeterías que caben en el encuadre automático: si entran todas
 *  en zonas muy densas el zoom baja tanto que los marcadores se solapan. */
const FIT_LIMIT = 15;

const markerIcon = L.divIcon({
  className: 'custom-marker',
  html: '<div class="marker-pin">☕</div>',
  iconSize: [38, 38],
  iconAnchor: [19, 19],
  popupAnchor: [0, -20],
});

const userMarkerIcon = L.divIcon({
  className: 'custom-marker',
  html: '<div class="user-dot" aria-hidden="true"></div>',
  iconSize: [22, 22],
  iconAnchor: [11, 11],
});

type MapViewProps = {
  cafes: CafeSummary[];
  selectedId: string | null;
  userLocation?: Coordinates | null;
  emptyMessage?: string;
  isLoading?: boolean;
  onSelect: (id: string) => void;
};

function MapA11y() {
  const map = useMap();

  useEffect(() => {
    const container = map.getContainer();
    const zoomIn = container.querySelector('.leaflet-control-zoom-in');
    const zoomOut = container.querySelector('.leaflet-control-zoom-out');

    zoomIn?.setAttribute('aria-label', 'Acercar el mapa');
    zoomIn?.setAttribute('title', 'Acercar');
    zoomOut?.setAttribute('aria-label', 'Alejar el mapa');
    zoomOut?.setAttribute('title', 'Alejar');
  }, [map]);

  return null;
}

function MapController({
  selectedId,
  userLocation,
  cafes,
}: {
  selectedId: string | null;
  userLocation: Coordinates | null;
  cafes: CafeSummary[];
}) {
  const map = useMap();
  const lastFitKey = useRef<string | null>(null);
  const lastFlownId = useRef<string | null>(null);

  useEffect(() => {
    // El encuadre se limita a los más cercanos: con muchas cafeterías en la
    // zona el zoom bajaba tanto que los marcadores se solapaban.
    const fitCafes = cafes.slice(0, FIT_LIMIT);
    const locationKey = userLocation
      ? `${userLocation.latitude},${userLocation.longitude}`
      : '';
    const fitKey = `${locationKey}|${fitCafes.map((cafe) => cafe.id).join(',')}`;

    if (lastFitKey.current === fitKey) {
      return;
    }
    lastFitKey.current = fitKey;

    if (fitCafes.length === 0) {
      // Sin resultados: encuadra la posición del usuario para que su punto
      // se vea aunque esté lejos de Madrid (datos de ejemplo).
      if (userLocation) {
        map.stop();
        map.setView([userLocation.latitude, userLocation.longitude], 14, {
          animate: true,
        });
      }
      return;
    }

    const points: [number, number][] = fitCafes.map(
      (cafe) => [cafe.latitude, cafe.longitude] as [number, number],
    );

    if (userLocation) {
      points.unshift([userLocation.latitude, userLocation.longitude]);
    }

    // Corta cualquier animación en curso (p. ej. un flyTo reciente) para que
    // el encuadre se aplique siempre de forma limpia.
    map.stop();
    map.fitBounds(L.latLngBounds(points), {
      padding: [48, 48],
      maxZoom: 15,
      animate: true,
    });
  }, [map, userLocation, cafes]);

  useEffect(() => {
    if (!selectedId || selectedId === lastFlownId.current) {
      return;
    }

    const cafe = cafes.find((item) => item.id === selectedId);
    if (!cafe) {
      return;
    }

    lastFlownId.current = selectedId;
    map.flyTo([cafe.latitude, cafe.longitude], Math.max(map.getZoom(), 15), {
      duration: 0.8,
    });
  }, [map, selectedId, cafes]);

  return null;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

/** Popup en HTML porque los marcadores se crean de forma imperativa. */
function popupHtml(cafe: CafeSummary): string {
  const rating =
    cafe.reviewCount > 0
      ? `<p class="mt-2 text-xs font-semibold text-caramel-600">★ ${cafe.rating
          .toFixed(1)
          .replace('.', ',')} · ${cafe.reviewCount} reseñas</p>`
      : `<p class="mt-2 text-xs text-coffee-500">Sin reseñas</p>`;

  return (
    `<div class="min-w-[180px]">` +
    `<p class="font-semibold text-coffee-950">${escapeHtml(cafe.name)}</p>` +
    `<p class="mt-1 text-xs text-coffee-500">${escapeHtml(cafe.address)}</p>` +
    `${rating}</div>`
  );
}

/**
 * Marcadores agrupados: con datos reales la densidad de cafeterías hace que
 * los pines se solapen, así que se agrupan en contadores que se abren al
 * acercar el mapa.
 */
function CafeClusterLayer({
  cafes,
  onSelect,
}: {
  cafes: CafeSummary[];
  onSelect: (id: string) => void;
}) {
  const map = useMap();
  const onSelectRef = useRef(onSelect);

  useEffect(() => {
    onSelectRef.current = onSelect;
  }, [onSelect]);

  useEffect(() => {
    const group = L.markerClusterGroup({
      // 45 px cubre el ancho del pin (38 px): cualquier par que se solaparía
      // queda agrupado, mientras que los mocks del demo (≥380 m) siguen sueltos.
      maxClusterRadius: 45,
      showCoverageOnHover: false,
      iconCreateFunction: (cluster) =>
        L.divIcon({
          html: `<div class="cluster-badge">${cluster.getChildCount()}</div>`,
          className: 'custom-marker',
          iconSize: [40, 40],
          iconAnchor: [20, 20],
        }),
    });

    for (const cafe of cafes) {
      const marker = L.marker([cafe.latitude, cafe.longitude], {
        icon: markerIcon,
        keyboard: false,
        title: cafe.name,
      });
      marker.bindPopup(popupHtml(cafe));
      marker.on('click', () => onSelectRef.current(cafe.id));
      group.addLayer(marker);
    }

    map.addLayer(group);

    return () => {
      map.removeLayer(group);
    };
  }, [map, cafes]);

  return null;
}

export function MapView({
  cafes,
  selectedId,
  userLocation,
  emptyMessage = 'No hay cafeterías en el mapa con estos filtros.',
  isLoading = false,
  onSelect,
}: MapViewProps) {
  return (
    <section
      className="relative h-full min-h-[320px] overflow-hidden rounded-3xl border border-coffee-100 shadow-soft"
      role="region"
      aria-label="Mapa de cafeterías"
    >
      <MapContainer
        center={MADRID_CENTER}
        zoom={13}
        scrollWheelZoom
        className="h-full w-full"
        aria-label="Mapa interactivo de cafeterías"
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <MapController selectedId={selectedId} userLocation={userLocation ?? null} cafes={cafes} />
        <MapA11y />

        {userLocation && (
          <Marker
            position={[userLocation.latitude, userLocation.longitude]}
            icon={userMarkerIcon}
            title="Tu ubicación"
          />
        )}

        <CafeClusterLayer cafes={cafes} onSelect={onSelect} />
      </MapContainer>

      {cafes.length === 0 && (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center bg-cream-50/80 backdrop-blur-sm">
          <p className="rounded-2xl bg-white px-5 py-3 text-sm font-medium text-coffee-700 shadow-soft">
            {isLoading ? 'Buscando cafeterías cerca de ti…' : emptyMessage}
          </p>
        </div>
      )}
    </section>
  );
}
