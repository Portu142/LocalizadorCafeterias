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

export type Review = {
  id: string;
  author: string;
  rating: number;
  comment: string;
  createdAt: string;
};

export type CafeDetail = CafeSummary & {
  phone: string;
  hours: string;
  menu: MenuItem[];
  reviews: Review[];
};

export type CreateReviewRequest = {
  author: string;
  rating: number;
  comment: string;
};
