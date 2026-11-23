import { useState } from 'react';
import { resolvePhotoUrl } from '../api/cafes';
import type { CafePhoto } from '../types/cafe';

type CafePhotosProps = {
  cafeName: string;
  photos: CafePhoto[];
};

/**
 * Tira de fotos con atribución. Las imágenes son de terceros (Wikimedia
 * Commons): se cargan en diferido y, si alguna falla, se oculta sin dejar un
 * hueco roto.
 */
export function CafePhotos({ cafeName, photos }: CafePhotosProps) {
  const [failed, setFailed] = useState<Set<string>>(() => new Set());
  const visible = photos.filter((photo) => !failed.has(photo.id));

  if (visible.length === 0) {
    return null;
  }

  return (
    <ul
      className="-mx-5 mb-4 flex snap-x snap-mandatory gap-2 overflow-x-auto px-5 [scrollbar-width:thin]"
      aria-label={`Fotos de ${cafeName}`}
    >
      {visible.map((photo, index) => (
        <li
          key={photo.id}
          className={
            visible.length === 1
              ? 'w-full shrink-0 snap-start'
              : 'w-[85%] shrink-0 snap-start sm:w-[70%]'
          }
        >
          <figure className="overflow-hidden rounded-2xl border border-coffee-100 bg-cream-100">
            <img
              src={resolvePhotoUrl(photo.url)}
              alt={`Foto ${index + 1} de ${cafeName}`}
              loading="lazy"
              decoding="async"
              width={960}
              height={640}
              className="h-36 w-full object-cover sm:h-40"
              onError={() =>
                setFailed((current) => new Set(current).add(photo.id))
              }
            />
            <figcaption className="truncate px-3 py-1.5 text-[11px] text-coffee-500">
              {photo.status === 'pending' && (
                <span className="mr-1.5 rounded-full bg-amber-50 px-2 py-0.5 font-semibold text-amber-800">
                  Pendiente de revisión
                </span>
              )}
              {/* Las fotos de la comunidad no tienen página de origen. */}
              {photo.sourceUrl ? (
                <a
                  href={photo.sourceUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="underline-offset-2 hover:text-coffee-800 hover:underline"
                >
                  Foto: {photo.author} · {photo.license}
                  <span className="sr-only"> (se abre en una pestaña nueva)</span>
                </a>
              ) : (
                <>
                  Foto: {photo.author} · {photo.license}
                </>
              )}
            </figcaption>
          </figure>
        </li>
      ))}
    </ul>
  );
}
