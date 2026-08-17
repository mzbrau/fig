import React, {useState} from 'react';
import clsx from 'clsx';
import useBaseUrl from '@docusaurus/useBaseUrl';
import styles from './home.module.css';

const TABS = [
  {
    id: 'settings',
    label: 'Settings',
    src: '/img/landing-page/app-screenshot.png',
    alt: 'Fig settings page with categorized editors for a connected service',
  },
  {
    id: 'dashboards',
    label: 'Dashboards',
    src: '/img/landing-page/dashboard.png',
    alt: 'Fig dashboard wallboard showing connected client uptime and health',
  },
  {
    id: 'clients',
    label: 'Clients',
    src: '/img/landing-page/connected-clients.png',
    alt: 'Connected clients table with health, live reload, and restart',
  },
  {
    id: 'assistant',
    label: 'Assistant',
    src: '/img/landing-page/fig-assistant.png',
    alt: 'Fig Assistant answering a question about setting history',
  },
  {
    id: 'reports',
    label: 'Reports',
    src: '/img/landing-page/sample-report.png',
    alt: 'Printable client uptime report generated from Fig',
  },
];

export default function ProductTheater() {
  const [active, setActive] = useState(0);
  const tab = TABS[active];
  const imageSrc = useBaseUrl(tab.src);

  const onKeyDown = (event) => {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') {
      return;
    }
    event.preventDefault();
    const delta = event.key === 'ArrowRight' ? 1 : -1;
    setActive((current) => (current + delta + TABS.length) % TABS.length);
  };

  return (
    <div className={styles.theater}>
      <div className={styles.browser}>
        <div className={styles.browserBar}>
          <span className={styles.traffic} aria-hidden="true">
            <i />
            <i />
            <i />
          </span>
          <span className={styles.browserTitle}>Fig Web</span>
        </div>
        <div
          className={styles.tabs}
          role="tablist"
          aria-label="Product views"
          onKeyDown={onKeyDown}>
          {TABS.map((item, index) => (
            <button
              key={item.id}
              type="button"
              role="tab"
              id={`theater-tab-${item.id}`}
              aria-selected={index === active}
              aria-controls={`theater-panel-${item.id}`}
              tabIndex={index === active ? 0 : -1}
              className={clsx(styles.tab, index === active && styles.tabActive)}
              onClick={() => setActive(index)}>
              {item.label}
            </button>
          ))}
        </div>
        <div
          className={styles.stage}
          role="tabpanel"
          id={`theater-panel-${tab.id}`}
          aria-labelledby={`theater-tab-${tab.id}`}>
          <img src={imageSrc} alt={tab.alt} />
        </div>
      </div>
    </div>
  );
}
