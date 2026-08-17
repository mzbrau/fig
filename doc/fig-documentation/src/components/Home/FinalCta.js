import React from 'react';
import Link from '@docusaurus/Link';
import styles from './home.module.css';

export default function FinalCta() {
  return (
    <section className={styles.final}>
      <h2 className={styles.heading}>Replace appsettings.json sprawl.</h2>
      <p className={styles.lead}>
        Add Fig.Client, run the API and Web, and manage every service from
        one place.
      </p>
      <div className={styles.heroActions}>
        <Link className={styles.btnPrimary} to="/docs/intro">
          Get Started
        </Link>
        <Link className={styles.btnGhost} to="/docs/guides/add-with-ai">
          Add with AI
        </Link>
      </div>
    </section>
  );
}
