import type { CafeDetail } from '../types/cafe';

/** Detalles de los cafés de OpenStreetMap ya consultados (Photon u Overpass),
 *  para abrir la ficha sin repetir llamadas a la red. */
const detailCache = new Map<string, CafeDetail>();

/** Ids cuyo detalle trae todas las etiquetas de OSM (servicios y fotos).
 *  Photon solo devuelve nombre y dirección, así que sus fichas se completan
 *  con Overpass al abrirlas. */
const completeIds = new Set<string>();

export function getCachedCafe(id: string): CafeDetail | undefined {
  return detailCache.get(id);
}

export function isCompleteInCache(id: string): boolean {
  return completeIds.has(id);
}

export function storeCafeDetails(cafe: CafeDetail, complete: boolean): void {
  // Una ficha completa nunca se sustituye por otra parcial de Photon.
  if (!complete && completeIds.has(cafe.id)) {
    return;
  }

  detailCache.set(cafe.id, cafe);
  if (complete) {
    completeIds.add(cafe.id);
  }
}
