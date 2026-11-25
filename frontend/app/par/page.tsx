import type { Metadata } from "next";
import Link from "next/link";
import { connection } from "next/server";
import { getStores } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import { STORE_HOSTS, isSafeExternalUrl } from "@/lib/query";

export const metadata: Metadata = { title: "Kā tas strādā?" };

const STATUS: Record<string, string> = {
  succeeded: "izdevās",
  partiallysucceeded: "daļēji izdevās",
  failed: "neizdevās",
  running: "notiek",
};

export default async function About() {
  await connection();
  const stores = await getStores().catch(() => null);

  return (
    <main className="container prose">
      <p>
        <Link href="/">‹ Atpakaļ uz precēm</Link>
      </p>
      <h1>Kā tas strādā?</h1>
      <p>
        Šī lapa automātiski apkopo preces no Rīgas interneta veikaliem, kurām veikals piedāvā <strong>bezprocentu līzingu vai nomaksu</strong>{" "}
        (aizņēmuma likme 0%). Uzsvars ir uz datortehniku, lielo sadzīves tehniku un elektrotehniku.
      </p>
      <h2>Cik bieži dati tiek atjaunoti?</h2>
      <p>
        Katrs veikals tiek pārbaudīts vismaz divas reizes dienā. Ja piedāvājums veikalā beidzas, prece pazūd no saraksta nākamajā pārbaudē.
        Prece netiek rādīta, ja tās piedāvājums nav apstiprināts pēdējo 36 stundu laikā.
      </p>
      <h2>Divu veidu piedāvājumi</h2>
      <ul>
        <li>
          <strong>0% akcija precei</strong> — veikals konkrētai precei rāda 0% likmi (piem., līzinga kalkulatorā).
        </li>
        <li>
          <strong>Veikala 0% piedāvājums</strong> — veikals piedāvā bezprocentu kredītu visām precēm noteiktā cenu robežā (piem., M79 uz 3
          mēnešiem, Tet nomaksa uz 3, 6 vai 12 mēnešiem). Cenu robeža un termiņš ir norādīti pie katras preces.
        </li>
      </ul>
      <p>
        Mēneša maksājums ir aprēķināts kā cena, dalīta ar garāko 0% termiņu. Tajā nav iekļautas iespējamās līguma vai komisijas maksas, tāpēc
        gada procentu likme (GPL) var būt lielāka par 0%. Kredītu piešķir kreditētājs pēc maksātspējas izvērtēšanas.
      </p>

      <h2>Veikali</h2>
      {stores ? (
        <table className="stores-table">
          <thead>
            <tr>
              <th>Veikals</th>
              <th>Statuss</th>
              <th>Pēdējā veiksmīgā atjaunošana</th>
              <th>Preces ar 0%</th>
            </tr>
          </thead>
          <tbody>
            {stores.map((s) => (
              <tr key={s.slug}>
                <td>
                  {isSafeExternalUrl(s.homepageUrl, [...STORE_HOSTS, "www.1a.lv", "www.rdveikals.lv", "www.dateks.lv", "www.ksenukai.lv", "220.lv", "www.balticdata.lv"]) ? (
                    <a href={s.homepageUrl} target="_blank" rel="noopener noreferrer nofollow">
                      {s.name}
                    </a>
                  ) : (
                    s.name
                  )}
                </td>
                <td>
                  {s.status === "active" ? `Aktīvs${s.lastRunStatus ? ` (pēdējā reize ${STATUS[s.lastRunStatus] ?? s.lastRunStatus})` : ""}` : s.note}
                </td>
                <td>{s.status === "active" ? formatDateTime(s.lastSuccessfulRunAt) : "—"}</td>
                <td>{s.status === "active" ? s.activeOffers : "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <p>Veikalu statuss pašlaik nav pieejams.</p>
      )}
      <p className="muted">
        Daļa populāru veikalu automatizētus pieprasījumus bloķē ar robotu aizsardzību. Šī lapa to neapiet, tāpēc šo veikalu preces netiek
        rādītas.
      </p>
    </main>
  );
}
