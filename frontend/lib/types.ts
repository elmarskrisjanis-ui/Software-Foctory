export type Product = {
  id: number;
  store: string;
  storeName: string;
  name: string;
  url: string;
  imageUrl: string | null;
  imageFallbackUrl: string | null;
  brand: string | null;
  category: string;
  categoryName: string;
  group: string;
  price: number;
  oldPrice: number | null;
  monthlyPayment: number;
  maxTermMonths: number;
  termsMonths: number[];
  interestRatePercent: number;
  offerScope: "product" | "store";
  provider: string;
  conditions: string;
  offerSourceUrl: string;
  verifiedAt: string;
};

export type ProductPage = { items: Product[]; total: number; page: number; pageSize: number };

export type FacetCount = { slug: string; name: string; group: string | null; count: number };

export type Facets = { groups: FacetCount[]; categories: FacetCount[]; stores: FacetCount[]; total: number };

export type StoreStatus = {
  slug: string;
  name: string;
  homepageUrl: string;
  status: "active" | "unavailable";
  note: string | null;
  lastSuccessfulRunAt: string | null;
  lastRunStatus: string | null;
  lastRunAt: string | null;
  interestFreeProgrammeActive: boolean | null;
  activeOffers: number;
};
