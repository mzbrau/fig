import React from 'react';
import styles from './home.module.css';

const ITEMS = [
  {title: 'Apache 2.0', body: 'Open source. No per-request cloud tax.'},
  {title: '.NET 10', body: 'Native configuration provider for ASP.NET.'},
  {title: 'Aspire', body: 'Run API and Web in your AppHost in one line.'},
  {title: 'Encrypted at rest', body: 'Setting values are encrypted in the database.'},
  {title: 'Secrets stay off the browser', body: 'Secret settings are never sent to Fig Web.'},
  {title: 'Offline cache', body: 'Clients start even if the API is unreachable.'},
];

export default function ProofStrip() {
  return (
    <section className={styles.proof} aria-label="Product facts">
      {ITEMS.map((item) => (
        <article key={item.title} className={styles.proofItem}>
          <h3>{item.title}</h3>
          <p>{item.body}</p>
        </article>
      ))}
    </section>
  );
}
