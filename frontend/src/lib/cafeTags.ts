import {
  Accessibility,
  Dog,
  Leaf,
  Plug,
  ShoppingBag,
  Umbrella,
  Wifi,
  type LucideIcon,
} from 'lucide-react';
import type { CafeTag } from '../types/cafe';

/** Texto e icono de cada etiqueta, en el orden en que se muestran. */
export const CAFE_TAGS: Record<CafeTag, { label: string; icon: LucideIcon }> = {
  wifi: { label: 'Wifi', icon: Wifi },
  terraza: { label: 'Terraza', icon: Umbrella },
  enchufes: { label: 'Enchufes', icon: Plug },
  accesible: { label: 'Accesible', icon: Accessibility },
  'para-llevar': { label: 'Para llevar', icon: ShoppingBag },
  mascotas: { label: 'Admite mascotas', icon: Dog },
  vegano: { label: 'Opciones veganas', icon: Leaf },
};

const ORDER = Object.keys(CAFE_TAGS) as CafeTag[];

/** Filtra claves desconocidas, quita duplicados y ordena. */
export function knownTags(tags: readonly string[] | undefined): CafeTag[] {
  const set = new Set(tags ?? []);
  return ORDER.filter((tag) => set.has(tag));
}
