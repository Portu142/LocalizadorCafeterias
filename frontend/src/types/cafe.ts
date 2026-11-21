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

export type CafeDetail = CafeSummary & {
  phone: string;
  hours: string;
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
