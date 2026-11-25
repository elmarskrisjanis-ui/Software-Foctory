import Link from "next/link";
import { buildHref, type Filters } from "@/lib/query";

export function Pagination({ filters, page, total, pageSize }: { filters: Filters; page: number; total: number; pageSize: number }) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  if (pages <= 1) return null;

  const window = new Set([1, pages, page - 2, page - 1, page, page + 1, page + 2].filter((p) => p >= 1 && p <= pages));
  const list = [...window].sort((a, b) => a - b);

  return (
    <nav className="pagination" aria-label="Lapas">
      {page > 1 && (
        <Link href={buildHref(filters, { page: String(page - 1) })} rel="prev">
          ‹ Iepriekšējā
        </Link>
      )}
      {list.map((p, i) => (
        <span key={p} className="page-item">
          {i > 0 && p - (list[i - 1] ?? p) > 1 && <span className="gap">…</span>}
          {p === page ? (
            <span className="current" aria-current="page">
              {p}
            </span>
          ) : (
            <Link href={buildHref(filters, { page: String(p) })}>{p}</Link>
          )}
        </span>
      ))}
      {page < pages && (
        <Link href={buildHref(filters, { page: String(page + 1) })} rel="next">
          Nākamā ›
        </Link>
      )}
    </nav>
  );
}
