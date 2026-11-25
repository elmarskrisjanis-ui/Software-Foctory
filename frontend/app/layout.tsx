import type { Metadata, Viewport } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: { default: "Bezprocentu — 0% līzings Rīgas e-veikalos", template: "%s — Bezprocentu" },
  description: "Preces ar bezprocentu līzingu vai nomaksu Rīgas interneta veikalos: datortehnika, lielā sadzīves tehnika un elektrotehnika.",
  robots: { index: false, follow: false },
};

export const viewport: Viewport = { themeColor: "#0f2a44" };

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="lv">
      <body>{children}</body>
    </html>
  );
}
