import type { CafeDetail } from '../types/cafe';

/** Detalles de los cafés de OpenStreetMap ya consultados (Photon u Overpass),
 *  para abrir la ficha sin repetir llamadas a la red. */
const detailCache = new Map<string, CafeDetail>();

export function getCachedCafe(id: string): CafeDetail | undefined {
  return detailCache.get(id);
}

export function storeCafeDetails(cafe: CafeDetail): void {
  detailCache.set(cafe.id, cafe);
}
