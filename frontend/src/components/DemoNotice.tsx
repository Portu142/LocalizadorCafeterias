import { MapPin } from 'lucide-react';

/**
 * Aviso del modo demo: los cafés son reales (OpenStreetMap), pero OSM no
 * publica cartas, así que el menú solo aparece si el local lo enlaza.
 */
export function DemoNotice() {
  return (
    <div className="mb-5 flex items-start gap-2.5 rounded-2xl border border-coffee-200 bg-cream-50 px-4 py-3">
      <span className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-caramel-500/15 text-caramel-700">
        <MapPin className="h-4 w-4" aria-hidden="true" />
      </span>
      <div className="text-sm">
        <p className="font-semibold text-coffee-900">
          Cafeterías reales de OpenStreetMap
        </p>
        <p className="mt-0.5 text-xs leading-5 text-coffee-600">
          Los datos se consultan en vivo según tu ubicación. Servicios (wifi,
          terraza…) y fotos aparecen cuando el local los tiene registrados en
          OpenStreetMap; las cartas no se publican allí, así que el menú
          aparece vacío. En la ficha tienes enlaces a OpenStreetMap y Google
          Maps para consultar la información real del local.
        </p>
      </div>
    </div>
  );
}
