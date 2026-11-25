import Link from "next/link";
import type { Facets } from "@/lib/types";
import { buildHref, type Filters } from "@/lib/query";

export function Sidebar({ filters, facets }: { filters: Filters; facets: Facets }) {
  const selectedGroup = filters.group ?? facets.categories.find((c) => c.slug === filters.category)?.group ?? undefined;
  const groups = selectedGroup ? facets.groups.filter((g) => g.slug === selectedGroup) : facets.groups;

  return (
    <aside className="sidebar" aria-label="Filtri">
      <section className="filter-box">
        <h2>Kategorijas</h2>
        {groups.map((g) => (
          <div key={g.slug} className="filter-group">
            <Link href={buildHref(filters, { group: g.slug })} className={`group-link ${filters.group === g.slug ? "active" : ""}`}>
              {g.name}
            </Link>
            <ul>
              {facets.categories
                .filter((c) => c.group === g.slug)
                .map((c) => (
                  <li key={c.slug}>
                    <Link
                      href={buildHref(filters, { category: filters.category === c.slug ? undefined : c.slug, group: filters.category === c.slug ? g.slug : undefined })}
                      className={`${filters.category === c.slug ? "active" : ""} ${c.count === 0 ? "empty" : ""}`}
                      aria-current={filters.category === c.slug ? "page" : undefined}
                    >
                      <span>{c.name}</span>
                      <span className="count">{c.count}</span>
                    </Link>
                  </li>
                ))}
            </ul>
          </div>
        ))}
      </section>

      <section className="filter-box">
        <h2>Veikals</h2>
        <ul>
          <li>
            <Link href={buildHref(filters, { store: undefined })} className={!filters.store ? "active" : ""}>
              <span>Visi veikali</span>
            </Link>
          </li>
          {facets.stores.map((s) => (
            <li key={s.slug}>
              <Link href={buildHref(filters, { store: s.slug })} className={`${filters.store === s.slug ? "active" : ""} ${s.count === 0 ? "empty" : ""}`}>
                <span>{s.name}</span>
                <span className="count">{s.count}</span>
              </Link>
            </li>
          ))}
        </ul>
      </section>

      <section className="filter-box">
        <h2>Cena, €</h2>
        <form action="/" method="get" className="price-form">
          {(["group", "category", "store", "q", "sort"] as const).map((k) =>
            filters[k] ? <input key={k} type="hidden" name={k} value={filters[k]} /> : null,
          )}
          <label>
            <span className="sr-only">No</span>
            <input name="minPrice" type="number" min={0} step="1" inputMode="numeric" placeholder="no" defaultValue={filters.minPrice ?? ""} />
          </label>
          <span aria-hidden="true">–</span>
          <label>
            <span className="sr-only">Līdz</span>
            <input name="maxPrice" type="number" min={0} step="1" inputMode="numeric" placeholder="līdz" defaultValue={filters.maxPrice ?? ""} />
          </label>
          <button type="submit">OK</button>
        </form>
        {(filters.minPrice || filters.maxPrice) && (
          <Link className="clear-link" href={buildHref(filters, { minPrice: undefined, maxPrice: undefined })}>
            Notīrīt cenu
          </Link>
        )}
      </section>
    </aside>
  );
}
