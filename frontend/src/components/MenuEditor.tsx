import { useState, type FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { CheckCircle2, Loader2, Plus } from 'lucide-react';
import { ApiError, addMenuItem, deleteMenuItem, ensureApiCafeId } from '../api/cafes';
import { USE_API } from '../config';
import { useCurrentUser, useForgetUser } from '../hooks/useAuth';

type MenuEditorProps = {
  cafeId: string;
  /** Abre la ventana de inicio de sesión. */
  onRequestLogin: () => void;
  /** El café era de OpenStreetMap y se acaba de guardar en la API con este id. */
  onCafeImported: (newId: string) => void;
};

/** «2,50» o «2.5» → 2.5; null si no es un precio válido (0,01–500 €, 2 decimales). */
function parsePrice(value: string): number | null {
  const normalized = value.trim().replace(',', '.');
  if (!/^\d{1,3}(\.\d{1,2})?$/.test(normalized)) {
    return null;
  }

  const price = Number(normalized);
  return price > 0 && price <= 500 ? price : null;
}

const inputClass =
  'w-full rounded-xl border border-coffee-200 bg-cream-50 px-3 py-2 text-sm text-coffee-950 outline-none transition placeholder:text-coffee-500 focus:border-caramel-400 focus:ring-2 focus:ring-caramel-400/20 aria-[invalid=true]:border-rose-400';

/**
 * Formulario para que la comunidad complete la carta. Si el café viene de
 * OpenStreetMap, primero se guarda en la API (que descarga sus datos de OSM)
 * y después se añade el plato.
 */
export function MenuEditor({ cafeId, onRequestLogin, onCafeImported }: MenuEditorProps) {
  const { data: user } = useCurrentUser();
  const forgetUser = useForgetUser();
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [price, setPrice] = useState('');
  const [errors, setErrors] = useState<{ name?: string; price?: string }>({});
  const [added, setAdded] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: async ({ dish, amount }: { dish: string; amount: number }) => {
      const targetId = await ensureApiCafeId(cafeId);

      await addMenuItem(targetId, dish, amount);
      return targetId;
    },
    onSuccess: async (targetId, { dish }) => {
      setName('');
      setPrice('');
      setAdded(dish);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['cafe', targetId] }),
        queryClient.invalidateQueries({ queryKey: ['cafes'] }),
      ]);
      if (targetId !== cafeId) {
        onCafeImported(targetId);
      }
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 401) {
        forgetUser();
      }
    },
  });

  if (!USE_API) {
    return null;
  }

  if (!user) {
    return (
      <button
        type="button"
        onClick={onRequestLogin}
        className="mt-4 inline-flex items-center gap-1.5 py-1 text-xs font-semibold text-caramel-600 hover:text-caramel-700"
      >
        <Plus className="h-3.5 w-3.5" aria-hidden="true" />
        Inicia sesión para añadir platos a la carta
      </button>
    );
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setAdded(null);

    const dish = name.trim();
    const amount = parsePrice(price);
    const next: typeof errors = {};

    if (dish.length < 2) {
      next.name = 'Escribe el nombre del plato (mínimo 2 caracteres).';
    }
    if (amount === null) {
      next.price = 'Escribe un precio entre 0,01 y 500 € (p. ej. 2,50).';
    }

    setErrors(next);
    if (next.name) {
      document.getElementById('menu-dish-name')?.focus();
      return;
    }
    if (next.price || amount === null) {
      document.getElementById('menu-dish-price')?.focus();
      return;
    }

    mutation.mutate({ dish, amount });
  };

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      className="mt-5 rounded-2xl border border-dashed border-coffee-200 bg-white p-4"
    >
      <h3 className="text-sm font-semibold text-coffee-950">Añade un plato a la carta</h3>
      <p className="mt-1 text-xs text-coffee-600">
        ¿Has estado aquí? Ayuda a completar la carta con lo que tomaste y su precio.
        Un administrador lo revisará antes de publicarlo.
      </p>

      <div className="mt-3 grid gap-3 sm:grid-cols-[minmax(0,1fr)_7rem]">
        <div>
          <label htmlFor="menu-dish-name" className="mb-1 block text-xs font-medium text-coffee-700">
            Plato o bebida
          </label>
          <input
            id="menu-dish-name"
            value={name}
            onChange={(event) => {
              setName(event.target.value);
              setErrors((current) => ({ ...current, name: undefined }));
              mutation.reset();
            }}
            maxLength={120}
            autoComplete="off"
            aria-invalid={Boolean(errors.name)}
            aria-describedby={errors.name ? 'menu-dish-name-error' : undefined}
            className={inputClass}
            placeholder="Ej. Flat white"
          />
          {errors.name && (
            <p id="menu-dish-name-error" className="mt-1 text-xs text-rose-700">
              {errors.name}
            </p>
          )}
        </div>

        <div>
          <label htmlFor="menu-dish-price" className="mb-1 block text-xs font-medium text-coffee-700">
            Precio (€)
          </label>
          <input
            id="menu-dish-price"
            value={price}
            onChange={(event) => {
              setPrice(event.target.value);
              setErrors((current) => ({ ...current, price: undefined }));
              mutation.reset();
            }}
            inputMode="decimal"
            maxLength={6}
            autoComplete="off"
            aria-invalid={Boolean(errors.price)}
            aria-describedby={errors.price ? 'menu-dish-price-error' : undefined}
            className={inputClass}
            placeholder="2,50"
          />
          {errors.price && (
            <p id="menu-dish-price-error" className="mt-1 text-xs text-rose-700">
              {errors.price}
            </p>
          )}
        </div>
      </div>

      {mutation.isError && (
        <p role="alert" className="mt-3 rounded-xl bg-rose-50 px-3 py-2 text-xs text-rose-700">
          {mutation.error.message}
        </p>
      )}

      {added && !mutation.isError && (
        <p
          role="status"
          className="mt-3 inline-flex items-center gap-1.5 rounded-xl bg-emerald-50 px-3 py-2 text-xs font-medium text-emerald-800"
        >
          <CheckCircle2 className="h-3.5 w-3.5" aria-hidden="true" />
          «{added}» enviado. Se publicará cuando lo apruebe un administrador.
        </p>
      )}

      <button
        type="submit"
        disabled={mutation.isPending}
        className="btn-primary mt-3 w-full disabled:cursor-not-allowed disabled:opacity-60 sm:w-auto"
      >
        {mutation.isPending ? (
          <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
        ) : (
          <Plus className="h-4 w-4" aria-hidden="true" />
        )}
        {mutation.isPending ? 'Añadiendo…' : 'Añadir plato'}
      </button>
    </form>
  );
}

/** Botón «Borrar» para los platos que ha añadido el propio usuario. */
export function DeleteDishButton({
  cafeId,
  itemId,
  itemName,
}: {
  cafeId: string;
  itemId: string;
  itemName: string;
}) {
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: () => deleteMenuItem(cafeId, itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['cafe', cafeId] }),
  });

  return (
    <span className="inline-flex items-center gap-2">
      <button
        type="button"
        onClick={() => {
          if (window.confirm(`¿Borrar «${itemName}» de la carta?`)) {
            mutation.mutate();
          }
        }}
        disabled={mutation.isPending}
        className="px-1 py-1 text-[11px] font-medium text-coffee-500 underline-offset-2 hover:text-rose-700 hover:underline disabled:opacity-60"
        aria-label={`Borrar ${itemName} de la carta`}
      >
        {mutation.isPending ? 'Borrando…' : 'Borrar'}
      </button>
      {mutation.isError && (
        <span role="alert" className="text-[11px] text-rose-700">
          {mutation.error.message}
        </span>
      )}
    </span>
  );
}
