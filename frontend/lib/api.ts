import "server-only";
import type { Facets, Product, ProductPage, StoreStatus } from "./types";
import { toApiQuery, type Filters } from "./query";

/**
 * The browser never talks to the API directly: pages are rendered on the server, which calls the API over the
 * internal network. API_BASE_URL must be configured; there is no silent fallback.
 */
function baseUrl(): string {
  const value = process.env.API_BASE_URL;
  if (!value) throw new Error("API_BASE_URL is not configured");
  const url = new URL(value);
  if (url.protocol !== "http:" && url.protocol !== "https:") throw new Error("API_BASE_URL must be http(s)");
  return url.toString().replace(/\/$/, "");
}

export class ApiError extends Error {
  constructor(public readonly status: number) {
    super(`API request failed with status ${status}`);
  }
}

async function get<T>(path: string): Promise<T> {
  const response = await fetch(`${baseUrl()}${path}`, {
    cache: "no-store",
    headers: { accept: "application/json" },
    signal: AbortSignal.timeout(10_000),
  });
  if (!response.ok) throw new ApiError(response.status);
  return (await response.json()) as T;
}

export const PAGE_SIZE = 24;

export function getProducts(filters: Filters): Promise<ProductPage> {
  return get<ProductPage>(`/api/products?${toApiQuery(filters, PAGE_SIZE)}`);
}

export function getFacets(filters: Filters): Promise<Facets> {
  const { page: _page, sort: _sort, ...rest } = filters;
  void _page;
  void _sort;
  return get<Facets>(`/api/facets?${toApiQuery(rest, 1)}`);
}

export function getProduct(id: number): Promise<Product> {
  return get<Product>(`/api/products/${id}`);
}

export function getStores(): Promise<StoreStatus[]> {
  return get<StoreStatus[]>("/api/stores");
}
