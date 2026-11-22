export type CafeSummary = {
  id: string;
  name: string;
  address: string;
  neighborhood: string;
  rating: number;
  reviewCount: number;
  latitude: number;
  longitude: number;
};

export type MenuItem = {
  id: string;
  name: string;
  price: number;
};

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
};

export type CafeDetail = CafeSummary & {
  phone: string;
  hours: string;
  /** Puede faltar si la API es anterior a las etiquetas. */
  tags?: string[];
  photos?: CafePhoto[];
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
