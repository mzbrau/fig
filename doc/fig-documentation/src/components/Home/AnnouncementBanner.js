import React from 'react';
import styles from './home.module.css';

export default function AnnouncementBanner() {
  return (
    <a
      href="https://github.com/mzbrau/fig/releases"
      className={styles.banner}>
      <span className={styles.bannerKicker}>Fig 4.0</span>
      <span className={styles.bannerTitle}>Fig 4.0 is Now Available!</span>
      <span className={styles.bannerSubtitle}>
        Discover the exciting new features and improvements
      </span>
    </a>
  );
}
