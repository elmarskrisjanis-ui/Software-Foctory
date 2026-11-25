import type { Product } from "@/lib/types";
import { formatDateTime, formatEur } from "@/lib/format";
import { IMAGE_HOSTS, STORE_HOSTS, isSafeExternalUrl } from "@/lib/query";
import { ProductImage } from "./ProductImage";

export function ProductCard({ product: p }: { product: Product }) {
  const storeUrl = isSafeExternalUrl(p.url, STORE_HOSTS) ? p.url : null;
  const imageUrl = isSafeExternalUrl(p.imageUrl, IMAGE_HOSTS) ? p.imageUrl : null;
  const imageFallbackUrl = isSafeExternalUrl(p.imageFallbackUrl, IMAGE_HOSTS) ? p.imageFallbackUrl : null;
  const sourceUrl = isSafeExternalUrl(p.offerSourceUrl, STORE_HOSTS) ? p.offerSourceUrl : null;
  const discount = p.oldPrice && p.oldPrice > p.price ? Math.round((1 - p.price / p.oldPrice) * 100) : null;
  const terms = p.termsMonths.length > 1 ? p.termsMonths.join(" / ") : String(p.maxTermMonths);

  return (
    <article className="card">
      <div className="card-media">
        <ProductImage key={p.id} src={imageUrl} fallback={imageFallbackUrl} alt={p.name} />
        <span className="badge-zero">0% · {p.maxTermMonths} mēn.</span>
        {discount !== null && discount > 0 && <span className="badge-sale">−{discount}%</span>}
      </div>

      <div className="card-body">
        <div className="card-meta">
          <span className={`store-chip store-${p.store}`}>{p.storeName}</span>
          <span className="cat">{p.categoryName}</span>
        </div>

        <h3 className="card-title" title={p.name}>
          {storeUrl ? (
            <a href={storeUrl} target="_blank" rel="noopener noreferrer nofollow">
              {p.name}
            </a>
          ) : (
            p.name
          )}
        </h3>

        <div className="price-row">
          <span className="price">{formatEur(p.price)}</span>
          {p.oldPrice && p.oldPrice > p.price && <span className="old-price">{formatEur(p.oldPrice)}</span>}
        </div>

        <div className="monthly">
          <strong>{formatEur(p.monthlyPayment)}</strong>
          <span>/mēn. × {p.maxTermMonths}</span>
        </div>
        <p className="offer-kind">
          {p.offerScope === "product" ? "0% akcija šai precei" : "Veikala 0% piedāvājums"} · termiņš {terms} mēn. · {p.provider}
        </p>

        <details className="conditions">
          <summary>Nosacījumi</summary>
          <p>{p.conditions}</p>
          {sourceUrl && (
            <p>
              <a href={sourceUrl} target="_blank" rel="noopener noreferrer nofollow">
                Veikala nosacījumu lapa ↗
              </a>
            </p>
          )}
        </details>

        <div className="card-footer">
          <span className="verified" title="Kad piedāvājums pēdējo reizi pārbaudīts veikala lapā">
            Pārbaudīts {formatDateTime(p.verifiedAt)}
          </span>
          {storeUrl && (
            <a className="btn" href={storeUrl} target="_blank" rel="noopener noreferrer nofollow">
              Uz veikalu
            </a>
          )}
        </div>
      </div>
    </article>
  );
}
