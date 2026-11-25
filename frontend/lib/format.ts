const eur = new Intl.NumberFormat("lv-LV", { style: "currency", currency: "EUR", minimumFractionDigits: 2 });

export function formatEur(value: number): string {
  return eur.format(value);
}

const dateTime = new Intl.DateTimeFormat("lv-LV", {
  timeZone: "Europe/Riga",
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? "—" : dateTime.format(d);
}

export function monthsLabel(n: number): string {
  return `${n} mēn.`;
}
