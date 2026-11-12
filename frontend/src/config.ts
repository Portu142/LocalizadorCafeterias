export const API_URL =
  (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? '';

export const IS_DEMO_MODE = !API_URL;

export const GITHUB_URL =
  (import.meta.env.VITE_GITHUB_URL as string | undefined) ??
  'https://github.com';
