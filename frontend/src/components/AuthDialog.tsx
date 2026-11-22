import { useState, type FormEvent, type ReactNode } from 'react';
import { Loader2 } from 'lucide-react';
import { useLogin, useRegister } from '../hooks/useAuth';
import { cn } from '../lib/utils';
import { Modal } from './Modal';

export type AuthMode = 'login' | 'register';

type AuthDialogProps = {
  mode: AuthMode | null;
  onModeChange: (mode: AuthMode) => void;
  onClose: () => void;
};

type Errors = Partial<Record<'email' | 'password' | 'displayName', string>>;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const inputClass =
  'w-full rounded-xl border border-coffee-200 bg-white px-3 py-2 text-sm text-coffee-950 outline-none transition placeholder:text-coffee-500 focus:border-caramel-400 focus:ring-2 focus:ring-caramel-400/20 aria-[invalid=true]:border-rose-400';

/** Inicio de sesión y registro en la misma ventana, con pestañas. */
export function AuthDialog({ mode, onModeChange, onClose }: AuthDialogProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [errors, setErrors] = useState<Errors>({});
  const loginMutation = useLogin();
  const registerMutation = useRegister();

  const isRegister = mode === 'register';
  const mutation = isRegister ? registerMutation : loginMutation;

  const switchMode = (next: AuthMode) => {
    setErrors({});
    loginMutation.reset();
    registerMutation.reset();
    onModeChange(next);
  };

  const close = () => {
    setPassword('');
    setErrors({});
    loginMutation.reset();
    registerMutation.reset();
    onClose();
  };

  const validate = (): boolean => {
    const next: Errors = {};

    if (!EMAIL_PATTERN.test(email.trim())) {
      next.email = 'Escribe un email válido.';
    }
    if (isRegister) {
      if (displayName.trim().length < 2) {
        next.displayName = 'El nombre debe tener al menos 2 caracteres.';
      }
      if (password.length < 8 || !/\d/.test(password)) {
        next.password = 'Usa al menos 8 caracteres e incluye un número.';
      }
    } else if (!password) {
      next.password = 'Escribe tu contraseña.';
    }

    setErrors(next);

    // Lleva el foco al primer campo con error (en el orden del formulario).
    const firstInvalid = (['displayName', 'email', 'password'] as const).find((key) => next[key]);
    if (firstInvalid) {
      document.getElementById(`auth-${firstInvalid}`)?.focus();
    }

    return Object.keys(next).length === 0;
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!validate()) {
      return;
    }

    const onSuccess = () => close();

    if (isRegister) {
      registerMutation.mutate(
        { email: email.trim(), password, displayName: displayName.trim() },
        { onSuccess },
      );
    } else {
      loginMutation.mutate({ email: email.trim(), password }, { onSuccess });
    }
  };

  const field = (
    id: keyof Errors,
    label: string,
    input: ReactNode,
    hint?: string,
  ) => (
    <div>
      <label htmlFor={`auth-${id}`} className="mb-1 block text-xs font-medium text-coffee-700">
        {label}
      </label>
      {input}
      {errors[id] ? (
        <p id={`auth-${id}-error`} className="mt-1 text-xs text-rose-700">
          {errors[id]}
        </p>
      ) : (
        hint && (
          <p id={`auth-${id}-hint`} className="mt-1 text-[11px] text-coffee-500">
            {hint}
          </p>
        )
      )}
    </div>
  );

  const describedBy = (id: keyof Errors, hasHint = false) =>
    errors[id] ? `auth-${id}-error` : hasHint ? `auth-${id}-hint` : undefined;

  return (
    <Modal
      open={mode !== null}
      title={isRegister ? 'Crea tu cuenta' : 'Inicia sesión'}
      onClose={close}
    >
      <div className="mb-4 grid grid-cols-2 gap-1 rounded-xl bg-cream-100 p-1" role="tablist">
        {(['login', 'register'] as const).map((tab) => (
          <button
            key={tab}
            type="button"
            role="tab"
            aria-selected={mode === tab}
            onClick={() => switchMode(tab)}
            className={cn(
              'rounded-lg px-3 py-2 text-xs font-semibold transition',
              mode === tab ? 'bg-white text-coffee-950 shadow-sm' : 'text-coffee-600 hover:text-coffee-900',
            )}
          >
            {tab === 'login' ? 'Iniciar sesión' : 'Crear cuenta'}
          </button>
        ))}
      </div>

      <p className="mb-4 text-xs leading-5 text-coffee-600">
        {isRegister
          ? 'Con una cuenta tus reseñas llevan tu nombre, puedes subir fotos y ver todas tus reseñas.'
          : 'Entra para reseñar cafeterías, subir fotos y ver tus reseñas.'}
      </p>

      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        {isRegister &&
          field(
            'displayName',
            'Nombre público',
            <input
              id="auth-displayName"
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              maxLength={40}
              autoComplete="nickname"
              aria-invalid={Boolean(errors.displayName)}
              aria-describedby={describedBy('displayName', true)}
              className={inputClass}
              placeholder="Ej. Ana"
            />,
            'Es el nombre que verán los demás en tus reseñas.',
          )}

        {field(
          'email',
          'Email',
          <input
            id="auth-email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            maxLength={254}
            autoComplete="email"
            inputMode="email"
            aria-invalid={Boolean(errors.email)}
            aria-describedby={describedBy('email')}
            className={inputClass}
            placeholder="tu@email.com"
          />,
        )}

        {field(
          'password',
          'Contraseña',
          <input
            id="auth-password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            maxLength={128}
            autoComplete={isRegister ? 'new-password' : 'current-password'}
            aria-invalid={Boolean(errors.password)}
            aria-describedby={describedBy('password', isRegister)}
            className={inputClass}
          />,
          isRegister ? 'Mínimo 8 caracteres con al menos un número.' : undefined,
        )}

        {mutation.isError && (
          <p role="alert" className="rounded-xl bg-rose-50 px-3 py-2 text-xs text-rose-700">
            {mutation.error.message}
          </p>
        )}

        <button
          type="submit"
          disabled={mutation.isPending}
          className="btn-primary w-full disabled:cursor-not-allowed disabled:opacity-60"
        >
          {mutation.isPending && <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />}
          {isRegister ? 'Crear cuenta' : 'Entrar'}
        </button>
      </form>
    </Modal>
  );
}
