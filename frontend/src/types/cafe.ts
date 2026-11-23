export type CafeSummary = {
  id: string;
  name: string;
  address: string;
  neighborhood: string;
  rating: number;
  reviewCount: number;
  latitude: number;
  longitude: number;
  /** Servicios (wifi, terraza…) para los filtros. Puede faltar si no hay datos. */
  tags?: string[];
  /** Elemento de OSM del que procede un café guardado en la API («node/123»). */
  osmId?: string;
  /** Horario en formato OSM o «Lun-Vie 08:00-20:00 · …»; vacío si no se conoce. */
  hours?: string;
};

export type MenuItem = {
  id: string;
  name: string;
  price: number;
  /** Quién lo añadió (vacío en los datos de ejemplo). */
  addedByUserId?: string | null;
  /** «pending» solo lo recibe su autor mientras espera revisión. */
  status?: ModerationStatus;
};

export type ModerationStatus = 'approved' | 'pending' | 'rejected';

export type MenuItemRating = {
  menuItemId: string;
  menuItemName: string;
  score: number;
};

export type Review = {
  id: string;
  author: string;
  rating: number;
  comment: string;
  createdAt: string;
  /** Puntuación de cada plato. Puede faltar en reseñas antiguas guardadas en localStorage. */
  itemRatings?: MenuItemRating[];
};

/** Claves del vocabulario de etiquetas (ver lib/cafeTags.ts). */
export type CafeTag =
  | 'wifi'
  | 'terraza'
  | 'enchufes'
  | 'accesible'
  | 'para-llevar'
  | 'mascotas'
  | 'vegano';

export type CafePhoto = {
  id: string;
  url: string;
  author: string;
  license: string;
  /** Página de origen (autor y licencia completos). */
  sourceUrl: string;
  status?: ModerationStatus;
};

export type CafeDetail = CafeSummary & {
  phone: string;
  hours: string;
  photos?: CafePhoto[];
  /** Web del local y enlace directo a su carta (de OpenStreetMap). */
  website?: string;
  menuUrl?: string;
  menu: MenuItem[];
  reviews: Review[];
};

export type CreateMenuItemRating = {
  menuItemId: string;
  score: number;
};

export type CreateReviewRequest = {
  author: string;
  rating: number;
  comment: string;
  itemRatings: CreateMenuItemRating[];
};

export type User = {
  id: string;
  email: string;
  displayName: string;
  isAdmin?: boolean;
};

export type UserReview = {
  id: string;
  cafeId: string;
  cafeName: string;
  rating: number;
  comment: string;
  createdAt: string;
};
