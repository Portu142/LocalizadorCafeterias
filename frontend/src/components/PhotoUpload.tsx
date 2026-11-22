import { useRef, useState, type ChangeEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Camera, CheckCircle2, Loader2 } from 'lucide-react';
import { ApiError, uploadPhoto } from '../api/cafes';
import { useCurrentUser, useForgetUser } from '../hooks/useAuth';

type PhotoUploadProps = {
  cafeId: string;
  onRequestLogin: () => void;
};

const MAX_BYTES = 5 * 1024 * 1024;
const ACCEPTED = ['image/jpeg', 'image/png', 'image/webp'];

/**
 * Subida de fotos de la comunidad (solo cafés de la API y con sesión). La
 * comprobación de tipo y tamaño aquí es para avisar rápido: la API vuelve a
 * validar el archivo por su contenido real.
 */
export function PhotoUpload({ cafeId, onRequestLogin }: PhotoUploadProps) {
  const { data: user } = useCurrentUser();
  const forgetUser = useForgetUser();
  const queryClient = useQueryClient();
  const inputRef = useRef<HTMLInputElement>(null);
  const [clientError, setClientError] = useState<string | null>(null);
  const [uploaded, setUploaded] = useState(false);

  const mutation = useMutation({
    mutationFn: (file: File) => uploadPhoto(cafeId, file),
    onSuccess: async () => {
      setUploaded(true);
      await queryClient.invalidateQueries({ queryKey: ['cafe', cafeId] });
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 401) {
        forgetUser();
      }
    },
  });

  const linkClass =
    'inline-flex items-center gap-1.5 py-1 text-xs font-semibold text-caramel-600 transition hover:text-caramel-700 disabled:cursor-wait disabled:opacity-60';

  if (!user) {
    return (
      <button type="button" onClick={onRequestLogin} className={linkClass}>
        <Camera className="h-3.5 w-3.5" aria-hidden="true" />
        Inicia sesión para subir fotos
      </button>
    );
  }

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    // Se vacía para poder volver a elegir el mismo archivo tras un error.
    event.target.value = '';
    setUploaded(false);
    mutation.reset();

    if (!file) {
      return;
    }
    if (!ACCEPTED.includes(file.type)) {
      setClientError('Solo se admiten fotos JPEG, PNG o WebP.');
      return;
    }
    if (file.size > MAX_BYTES) {
      setClientError('La foto debe pesar como máximo 5 MB.');
      return;
    }

    setClientError(null);
    mutation.mutate(file);
  };

  const error = clientError ?? (mutation.isError ? mutation.error.message : null);

  return (
    <span className="inline-flex flex-wrap items-center gap-x-3 gap-y-1">
      <input
        ref={inputRef}
        type="file"
        accept={ACCEPTED.join(',')}
        onChange={handleChange}
        className="sr-only"
        tabIndex={-1}
        aria-hidden="true"
      />
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        disabled={mutation.isPending}
        className={linkClass}
      >
        {mutation.isPending ? (
          <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden="true" />
        ) : (
          <Camera className="h-3.5 w-3.5" aria-hidden="true" />
        )}
        {mutation.isPending ? 'Subiendo foto…' : 'Subir una foto'}
      </button>

      <span aria-live="polite" className="text-xs">
        {error && <span className="text-rose-700">{error}</span>}
        {uploaded && !error && (
          <span className="inline-flex items-center gap-1 text-emerald-800">
            <CheckCircle2 className="h-3.5 w-3.5" aria-hidden="true" />
            Foto publicada
          </span>
        )}
      </span>
    </span>
  );
}
