"use client";

export default function Error({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <main className="container prose">
      <h1>Kaut kas nogāja greizi</h1>
      <p>Pašlaik neizdevās ielādēt preces. Lūdzu, mēģini vēlreiz pēc brīža.</p>
      <button type="button" className="btn" onClick={() => reset()}>
        Mēģināt vēlreiz
      </button>
    </main>
  );
}
