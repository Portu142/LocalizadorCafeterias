import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchCurrentUser, fetchMyReviews, login, logout, register } from '../api/cafes';
import { USE_API } from '../config';
import type { User } from '../types/cafe';

const ME_KEY = ['me'] as const;

/** Usuario con sesión iniciada (`null` sin sesión; siempre `null` sin API). */
export function useCurrentUser() {
  return useQuery({
    queryKey: ME_KEY,
    queryFn: fetchCurrentUser,
    enabled: USE_API,
    staleTime: Number.POSITIVE_INFINITY,
    retry: false,
  });
}

export function useLogin() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) =>
      login(email, password),
    onSuccess: (user: User) => queryClient.setQueryData(ME_KEY, user),
  });
}

export function useRegister() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      email,
      password,
      displayName,
    }: {
      email: string;
      password: string;
      displayName: string;
    }) => register(email, password, displayName),
    onSuccess: (user: User) => queryClient.setQueryData(ME_KEY, user),
  });
}

export function useLogout() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: logout,
    onSettled: () => {
      queryClient.setQueryData(ME_KEY, null);
      queryClient.removeQueries({ queryKey: ['my-reviews'] });
    },
  });
}

/** Marca la sesión como caducada (p. ej. tras un 401 de la API). */
export function useForgetUser() {
  const queryClient = useQueryClient();
  return () => queryClient.setQueryData(ME_KEY, null);
}

export function useMyReviews(enabled: boolean) {
  return useQuery({
    queryKey: ['my-reviews'],
    queryFn: fetchMyReviews,
    enabled,
  });
}
