import { useEffect, useRef, useState } from 'react';
import { ChevronDown, LogOut, MessageSquare, UserRound } from 'lucide-react';
import { USE_API } from '../config';
import { useCurrentUser, useLogout } from '../hooks/useAuth';

type AccountMenuProps = {
  onLogin: () => void;
  onMyReviews: () => void;
};

/** Botón de cuenta en la cabecera: «Entrar» o el menú del usuario. */
export function AccountMenu({ onLogin, onMyReviews }: AccountMenuProps) {
  const { data: user, isLoading } = useCurrentUser();
  const logoutMutation = useLogout();
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // Cierra el menú al hacer clic fuera o pulsar Escape.
  useEffect(() => {
    if (!open) {
      return;
    }

    const onPointerDown = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false);
      }
    };

    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('pointerdown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [open]);

  // Sin API (demo estática) no hay cuentas.
  if (!USE_API || isLoading) {
    return null;
  }

  if (!user) {
    return (
      <button
        type="button"
        onClick={onLogin}
        className="inline-flex items-center gap-1.5 rounded-full bg-coffee-900 px-3.5 py-2 text-xs font-semibold text-cream-50 transition hover:bg-coffee-800"
      >
        <UserRound className="h-3.5 w-3.5" aria-hidden="true" />
        Entrar
      </button>
    );
  }

  const itemClass =
    'flex w-full items-center gap-2 rounded-xl px-3 py-2 text-left text-sm text-coffee-800 transition hover:bg-cream-100';

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        aria-haspopup="true"
        className="inline-flex max-w-[11rem] items-center gap-1.5 rounded-full border border-coffee-200 bg-white/80 px-3 py-2 text-xs font-semibold text-coffee-800 transition hover:bg-white"
      >
        <UserRound className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
        <span className="truncate">{user.displayName}</span>
        <ChevronDown className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
      </button>

      {open && (
        <div className="absolute right-0 z-50 mt-2 w-52 rounded-2xl border border-coffee-100 bg-white p-1.5 shadow-soft">
          <p className="truncate px-3 py-1.5 text-[11px] text-coffee-500">{user.email}</p>
          <button
            type="button"
            className={itemClass}
            onClick={() => {
              setOpen(false);
              onMyReviews();
            }}
          >
            <MessageSquare className="h-4 w-4" aria-hidden="true" />
            Mis reseñas
          </button>
          <button
            type="button"
            className={itemClass}
            disabled={logoutMutation.isPending}
            onClick={() => {
              setOpen(false);
              logoutMutation.mutate();
            }}
          >
            <LogOut className="h-4 w-4" aria-hidden="true" />
            Cerrar sesión
          </button>
        </div>
      )}
    </div>
  );
}
