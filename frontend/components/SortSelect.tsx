"use client";

import { SORT_OPTIONS, type Filters } from "@/lib/query";

/** Works without JavaScript (submit button); with JavaScript the form submits on change. */
export function SortSelect({ filters }: { filters: Filters }) {
  return (
    <form action="/" method="get" className="sort-form">
      {(["group", "category", "store", "q", "minPrice", "maxPrice"] as const).map((k) =>
        filters[k] ? <input key={k} type="hidden" name={k} value={filters[k]} /> : null,
      )}
      <label htmlFor="sort">Kārtot:</label>
      <select id="sort" name="sort" defaultValue={filters.sort ?? "monthly-asc"} onChange={(e) => e.currentTarget.form?.requestSubmit()}>
        {SORT_OPTIONS.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
      <noscript>
        <button type="submit">Kārtot</button>
      </noscript>
    </form>
  );
}
