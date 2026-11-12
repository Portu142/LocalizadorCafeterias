import { useEffect, useMemo } from 'react';
import L from 'leaflet';
import { MapContainer, Marker, Popup, TileLayer, useMap } from 'react-leaflet';
import { Star } from 'lucide-react';
import type { CafeSummary } from '../types/cafe';

const MADRID_CENTER: [number, number] = [40.4168, -3.7038];

const markerIcon = L.divIcon({
  className: 'custom-marker',
  html: '<div class="marker-pin">☕</div>',
  iconSize: [38, 38],
  iconAnchor: [19, 19],
  popupAnchor: [0, -20],
});

type MapViewProps = {
  cafes: CafeSummary[];
  selectedId: string | null;
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

function MapController({ selected }: { selected: CafeSummary | undefined }) {
  const map = useMap();

  useEffect(() => {
    if (!selected) {
      return;
    }

    map.flyTo([selected.latitude, selected.longitude], Math.max(map.getZoom(), 15), {
      duration: 0.8,
    });
  }, [map, selected]);

  return null;
}

export function MapView({ cafes, selectedId, onSelect }: MapViewProps) {
  const selected = useMemo(
    () => cafes.find((cafe) => cafe.id === selectedId),
    [cafes, selectedId],
  );

  return (
    <section
      className="relative h-full min-h-[320px] overflow-hidden rounded-3xl border border-coffee-100 shadow-soft"
      role="region"
      aria-label="Mapa de cafeterías en Madrid"
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
        <MapController selected={selected} />
        <MapA11y />

        {cafes.map((cafe) => (
          <Marker
            key={cafe.id}
            position={[cafe.latitude, cafe.longitude]}
            icon={markerIcon}
            title={`${cafe.name} — ${cafe.neighborhood}`}
            alt={`${cafe.name}, ${cafe.address}`}
            eventHandlers={{ click: () => onSelect(cafe.id) }}
          >
            <Popup>
              <div className="min-w-[180px]">
                <p className="font-semibold text-coffee-950">{cafe.name}</p>
                <p className="mt-1 text-xs text-coffee-500">{cafe.address}</p>
                <p className="mt-2 inline-flex items-center gap-1 text-xs font-semibold text-caramel-600">
                  <Star className="h-3 w-3 fill-caramel-500 text-caramel-500" />
                  {cafe.rating.toFixed(1).replace('.', ',')} ·{' '}
                  {cafe.reviewCount} reseñas
                </p>
              </div>
            </Popup>
          </Marker>
        ))}
      </MapContainer>

      {cafes.length === 0 && (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center bg-cream-50/80 backdrop-blur-sm">
          <p className="rounded-2xl bg-white px-5 py-3 text-sm font-medium text-coffee-700 shadow-soft">
            No hay cafeterías en el mapa con estos filtros.
          </p>
        </div>
      )}
    </section>
  );
}
