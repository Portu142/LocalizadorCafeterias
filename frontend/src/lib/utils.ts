import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatPrice(value: number) {
  return new Intl.NumberFormat('es-ES', {
    style: 'currency',
    currency: 'EUR',
  }).format(value);
}

const DATE_FORMAT = new Intl.DateTimeFormat('es-ES', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
});

/** Fecha corta; una fecha inválida (p. ej. de localStorage) no rompe la ficha. */
export function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '' : DATE_FORMAT.format(date);
}

export function initials(name: string) {
  // Solo letras y números: un nombre como «<Ana>» no debe mostrar «<».
  const letters = name
    .split(/\s+/)
    .map((part) => part.match(/[\p{L}\p{N}]/u)?.[0] ?? '')
    .filter(Boolean);

  return (letters.length > 0 ? letters : ['?'])
    .join('')
    .slice(0, 2)
    .toUpperCase();
}

/**
 * UUID v4 aleatorio. `crypto.randomUUID` solo existe en páginas seguras
 * (HTTPS o localhost) y en navegadores recientes; si falta, se construye con
 * `crypto.getRandomValues`, disponible en todos los navegadores actuales.
 */
export function randomId(): string {
  if (typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
