/**
 * Storefront filters live in the URL so every view can be shared and bookmarked.
 * Only known keys with bounded, well-formed values are passed on to the API; the API validates again.
 */
export type Filters = {
  group?: string;
  category?: string;
  store?: string;
  q?: string;
  minPrice?: string;
  maxPrice?: string;
  sort?: string;
  page?: string;
};

export const SORT_OPTIONS = [
  { value: "monthly-asc", label: "Mazākais mēneša maksājums" },
  { value: "price-asc", label: "Lētākās vispirms" },
  { value: "price-desc", label: "Dārgākās vispirms" },
  { value: "term-desc", label: "Garākais 0% termiņš" },
  { value: "newest", label: "Jaunākie piedāvājumi" },
] as const;

const SLUG = /^[a-z0-9-]{1,60}$/;
const NUMBER = /^\d{1,7}(\.\d{1,2})?$/;
const PAGE = /^\d{1,3}$/;
const SORTS = new Set<string>(SORT_OPTIONS.map((s) => s.value));

type RawParams = Record<string, string | string[] | undefined>;

function first(value: string | string[] | undefined): string | undefined {
  const v = Array.isArray(value) ? value[0] : value;
  const trimmed = v?.trim();
  return trimmed ? trimmed : undefined;
}

export function parseFilters(params: RawParams): Filters {
  const filters: Filters = {};
  const group = first(params.group);
  const category = first(params.category);
  const store = first(params.store);
  const q = first(params.q);
  const minPrice = first(params.minPrice);
  const maxPrice = first(params.maxPrice);
  const sort = first(params.sort);
  const page = first(params.page);

  if (group && SLUG.test(group)) filters.group = group;
  if (category && SLUG.test(category)) filters.category = category;
  if (store && SLUG.test(store)) filters.store = store;
  if (q) filters.q = q.replace(/[\u0000-\u001f\u007f]/g, "").slice(0, 100);
  if (minPrice && NUMBER.test(minPrice)) filters.minPrice = minPrice;
  if (maxPrice && NUMBER.test(maxPrice)) filters.maxPrice = maxPrice;
  if (sort && SORTS.has(sort)) filters.sort = sort;
  if (page && PAGE.test(page) && Number(page) >= 1) filters.page = page;
  return filters;
}

/** Builds a storefront URL from current filters plus changes; changing any filter resets paging. */
export function buildHref(current: Filters, changes: Partial<Record<keyof Filters, string | undefined>>): string {
  const next: Filters = { ...current, ...changes };
  if (!("page" in changes)) delete next.page;
  if ("category" in changes && changes.category && !("group" in changes)) delete next.group;
  if ("group" in changes && !("category" in changes)) delete next.category;

  const sp = new URLSearchParams();
  for (const key of ["group", "category", "store", "q", "minPrice", "maxPrice", "sort", "page"] as const) {
    const value = next[key];
    if (value) sp.set(key, value);
  }
  const qs = sp.toString();
  return qs ? `/?${qs}` : "/";
}

export function toApiQuery(filters: Filters, pageSize = 24): string {
  const sp = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value) sp.set(key, value);
  }
  sp.set("pageSize", String(pageSize));
  return sp.toString();
}

/** Only https links to known store hosts are rendered as outbound links/images. */
export function isSafeExternalUrl(url: string | null | undefined, allowedHosts: readonly string[]): url is string {
  if (!url) return false;
  try {
    const u = new URL(url);
    return u.protocol === "https:" && u.username === "" && u.password === "" && allowedHosts.includes(u.hostname);
  } catch {
    return false;
  }
}

export const STORE_HOSTS = ["www.euronics.lv", "www.m79.lv", "www.tet.lv"] as const;
export const IMAGE_HOSTS = ["www.euronics.lv", "pictures.m79.lv", "cdn.tet.lv"] as const;
