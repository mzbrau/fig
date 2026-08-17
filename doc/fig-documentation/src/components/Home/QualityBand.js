import React from 'react';
import useBaseUrl from '@docusaurus/useBaseUrl';
import styles from './home.module.css';

export default function QualityBand() {
  const changes = useBaseUrl('/img/landing-page/safe-changes.png');
  const history = useBaseUrl('/img/landing-page/setting-history.png');

  return (
    <section className={styles.band}>
      <div className={styles.bandCopy}>
        <p className={styles.eyebrow}>Audit</p>
        <h2 className={styles.heading}>See what changed, and when.</h2>
        <p className={styles.lead}>
          Review the diff before you save. Then use history to see which
          values existed in each version — so you can audit a change or
          troubleshoot a breakage without guessing.
        </p>
      </div>
      <div className={styles.qualityGrid}>
        <figure className={styles.shot}>
          <img
            src={changes}
            alt="Save changes dialog showing diffs, validation, and an optional message"
          />
          <figcaption>Confirm the diff and leave a message.</figcaption>
        </figure>
        <figure className={styles.shot}>
          <img
            src={history}
            alt="Client history comparing setting values across versions"
          />
          <figcaption>Compare values across versions.</figcaption>
        </figure>
      </div>
    </section>
  );
}
