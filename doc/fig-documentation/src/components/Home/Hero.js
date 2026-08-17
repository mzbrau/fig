import React from 'react';
import Link from '@docusaurus/Link';
import useBaseUrl from '@docusaurus/useBaseUrl';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import styles from './home.module.css';

export default function Hero({children}) {
  const {siteConfig} = useDocusaurusContext();
  const logo = useBaseUrl('/img/landing-page/fig_logo_name_right.svg');

  return (
    <header className={styles.hero}>
      <div className={styles.heroGlow} aria-hidden="true" />
      <div className={styles.heroInner}>
        <div className={styles.logoStage}>
          <div className={styles.rings} aria-hidden="true">
            <span />
            <span />
            <span />
          </div>
          <img
            src={logo}
            alt="Fig"
            className={styles.logo}
          />
        </div>
        <p className={styles.tagline}>{siteConfig.tagline}</p>
        <p className={styles.thesis}>
          Define settings in code.<br />
          Manage them from one place.
        </p>
        <div className={styles.heroActions}>
          <Link className={styles.btnPrimary} to="/docs/intro">
            Get Started
          </Link>
          <Link className={styles.btnGhost} to="/docs/guides/add-with-ai">
            Add with AI
          </Link>
        </div>
        <ul className={styles.proofChips}>
          <li>.NET 10</li>
          <li>Apache 2.0</li>
          <li>Aspire</li>
        </ul>
      </div>
      {children}
    </header>
  );
}
