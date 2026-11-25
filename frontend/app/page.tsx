import Link from "next/link";
import { connection } from "next/server";
import { Header } from "@/components/Header";
import { Sidebar } from "@/components/Sidebar";
import { SortSelect } from "@/components/SortSelect";
import { ProductCard } from "@/components/ProductCard";
import { Pagination } from "@/components/Pagination";
import { Footer } from "@/components/Footer";
import { getFacets, getProducts, getStores, PAGE_SIZE } from "@/lib/api";
import { buildHref, parseFilters } from "@/lib/query";

type Props = { searchParams: Promise<Record<string, string | string[] | undefined>> };

export default async function Home({ searchParams }: Props) {
  await connection();
  const filters = parseFilters(await searchParams);
  const [products, facets, stores] = await Promise.all([getProducts(filters), getFacets(filters), getStores().catch(() => null)]);

  const category = facets.categories.find((c) => c.slug === filters.category);
  const group = facets.groups.find((g) => g.slug === (filters.group ?? category?.group));
  const store = facets.stores.find((s) => s.slug === filters.store);
  const title = category?.name ?? group?.name ?? "Visas preces ar 0% līzingu";
  const activeChips = [
    store && { label: `Veikals: ${store.name}`, href: buildHref(filters, { store: undefined }) },
    filters.q && { label: `“${filters.q}”`, href: buildHref(filters, { q: undefined }) },
    (filters.minPrice || filters.maxPrice) && {
      label: `Cena ${filters.minPrice ?? "0"}–${filters.maxPrice ?? "∞"} €`,
      href: buildHref(filters, { minPrice: undefined, maxPrice: undefined }),
    },
  ].filter((c): c is { label: string; href: string } => !!c);

  return (
    <>
      <Header filters={filters} groups={facets.groups} />
      <main className="container layout">
        <Sidebar filters={filters} facets={facets} />

        <section className="results" aria-labelledby="results-title">
          <nav className="breadcrumbs" aria-label="Ceļš">
            <Link href="/">Sākums</Link>
            {group && (
              <>
                {" › "}
                <Link href={buildHref({}, { group: group.slug })}>{group.name}</Link>
              </>
            )}
            {category && <> › {category.name}</>}
          </nav>

          <div className="results-head">
            <div>
              <h1 id="results-title">{title}</h1>
              <p className="muted">
                {products.total} {products.total === 1 ? "prece" : "preces"} ar bezprocentu līzingu vai nomaksu
              </p>
            </div>
            <SortSelect filters={filters} />
          </div>

          {activeChips.length > 0 && (
            <div className="chips">
              {activeChips.map((c) => (
                <Link key={c.label} href={c.href} className="chip">
                  {c.label} <span aria-hidden="true">×</span>
                  <span className="sr-only">noņemt filtru</span>
                </Link>
              ))}
              <Link href="/" className="chip clear">
                Notīrīt visu
              </Link>
            </div>
          )}

          {products.items.length === 0 ? (
            <div className="empty-state">
              <h2>Nekas netika atrasts</h2>
              <p>Pamēģini citu kategoriju vai noņem kādu filtru. Piedāvājumi tiek atjaunoti vairākas reizes dienā.</p>
            </div>
          ) : (
            <div className="grid">
              {products.items.map((p) => (
                <ProductCard key={p.id} product={p} />
              ))}
            </div>
          )}

          <Pagination filters={filters} page={products.page} total={products.total} pageSize={PAGE_SIZE} />
        </section>
      </main>
      <Footer stores={stores} />
    </>
  );
}
