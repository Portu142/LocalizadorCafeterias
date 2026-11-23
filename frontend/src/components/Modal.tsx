import { useEffect, useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';

type ModalProps = {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  /** «wide» para contenido con tablas o rejillas (panel de administración). */
  size?: 'normal' | 'wide';
};

/**
 * Ventana modal sobre el elemento nativo <dialog>: el navegador se encarga
 * de atrapar el foco, cerrar con Escape y devolver el foco al botón que la
 * abrió, sin dependencias extra.
 */
export function Modal({ open, title, onClose, children, size = 'normal' }: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) {
      return;
    }

    if (open && !dialog.open) {
      dialog.showModal();
    } else if (!open && dialog.open) {
      dialog.close();
    }
  }, [open]);

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="modal-title"
      onClose={onClose}
      // Clic en el fondo oscuro (fuera del contenido) cierra la ventana.
      onClick={(event) => {
        if (event.target === dialogRef.current) {
          onClose();
        }
      }}
      className={`m-auto ${size === 'wide' ? 'w-[min(94vw,52rem)]' : 'w-[min(92vw,26rem)]'} rounded-3xl border border-coffee-100 bg-cream-50 p-0 text-coffee-950 shadow-soft backdrop:bg-coffee-950/40 backdrop:backdrop-blur-sm`}
    >
      {open && (
        <div className="p-6">
          <div className="mb-4 flex items-start justify-between gap-3">
            <h2 id="modal-title" className="font-display text-xl font-semibold">
              {title}
            </h2>
            <button
              type="button"
              onClick={onClose}
              className="-mr-2 -mt-1 rounded-full p-2 text-coffee-500 transition hover:bg-cream-100 hover:text-coffee-800"
              aria-label="Cerrar"
            >
              <X className="h-4 w-4" aria-hidden="true" />
            </button>
          </div>
          {children}
        </div>
      )}
    </dialog>
  );
}
