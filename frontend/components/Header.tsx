import Link from "next/link";
import type { Filters } from "@/lib/query";
import type { FacetCount } from "@/lib/types";
import { buildHref } from "@/lib/query";

export function Header({ filters, groups }: { filters: Filters; groups: FacetCount[] }) {
  const activeGroup = filters.group ?? undefined;
  return (
    <header className="site-header">
      <div className="topbar">
        <div className="container topbar-inner">
          <Link href="/" className="logo" aria-label="Bezprocentu — sākums">
            <span className="logo-badge">0%</span>
            <span className="logo-text">
              Bezprocentu<small>līzings Rīgas e-veikalos</small>
            </span>
          </Link>

          <form className="search" action="/" method="get" role="search">
            {filters.group && <input type="hidden" name="group" value={filters.group} />}
            {filters.category && <input type="hidden" name="category" value={filters.category} />}
            {filters.store && <input type="hidden" name="store" value={filters.store} />}
            {filters.sort && <input type="hidden" name="sort" value={filters.sort} />}
            <label htmlFor="q" className="sr-only">
              Meklēt preces
            </label>
            <input id="q" name="q" type="search" placeholder="Meklē, piem., televizors, MacBook, Bosch…" defaultValue={filters.q ?? ""} maxLength={100} />
            <button type="submit">Meklēt</button>
          </form>

          <nav className="top-links" aria-label="Informācija">
            <Link href="/par">Kā tas strādā?</Link>
          </nav>
        </div>
      </div>

      <nav className="catbar" aria-label="Preču grupas">
        <div className="container catbar-inner">
          <Link href={buildHref(filters, { group: undefined, category: undefined })} className={!activeGroup && !filters.category ? "active" : ""}>
            Visas preces
          </Link>
          {groups.map((g) => (
            <Link key={g.slug} href={buildHref(filters, { group: g.slug })} className={activeGroup === g.slug ? "active" : ""}>
              {g.name} <span className="count">{g.count}</span>
            </Link>
          ))}
        </div>
      </nav>
    </header>
  );
}
