import Link from "next/link";
import type { StoreStatus } from "@/lib/types";
import { formatDateTime } from "@/lib/format";

export function Footer({ stores }: { stores: StoreStatus[] | null }) {
  const active = stores?.filter((s) => s.status === "active") ?? [];
  const lastUpdate = active
    .map((s) => s.lastSuccessfulRunAt)
    .filter((d): d is string => !!d)
    .sort()
    .at(-1);

  return (
    <footer className="site-footer">
      <div className="container footer-inner">
        <div>
          <strong>Dati atjaunoti:</strong> {formatDateTime(lastUpdate)}
          <ul className="store-status">
            {active.map((s) => (
              <li key={s.slug} className={s.lastRunStatus === "failed" ? "warn" : ""}>
                {s.name}: {formatDateTime(s.lastSuccessfulRunAt)}
                {s.lastRunStatus === "failed" && " (pēdējā atjaunošana neizdevās)"}
              </li>
            ))}
          </ul>
        </div>
        <p className="disclaimer">
          Informācija tiek apkopota automātiski no veikalu publiskajām lapām un var mainīties. Pirms pirkuma pārbaudi aktuālos kredīta
          nosacījumus veikalā. Kredītu piešķir kreditētājs pēc maksātspējas izvērtēšanas. <Link href="/par">Vairāk informācijas</Link>
        </p>
      </div>
    </footer>
  );
}
