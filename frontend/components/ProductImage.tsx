"use client";

import { useState } from "react";

/** Shows the store image; if it fails, tries the store's fallback image, then a neutral placeholder. */
export function ProductImage({ src, fallback, alt }: { src: string | null; fallback: string | null; alt: string }) {
  const candidates = [src, fallback].filter((u): u is string => !!u);
  const [index, setIndex] = useState(0);
  const current = candidates[index];

  if (!current) {
    return (
      <div className="no-image" aria-hidden="true">
        Nav attēla
      </div>
    );
  }

  return (
    // eslint-disable-next-line @next/next/no-img-element -- images stay on the store CDN (allowed in CSP), no proxying
    <img src={current} alt={alt} loading="lazy" referrerPolicy="no-referrer" onError={() => setIndex((i) => i + 1)} />
  );
}
