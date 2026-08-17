import React, {useEffect, useRef, useState} from 'react';
import clsx from 'clsx';
import {usePrefersReducedMotion} from './hooks';
import styles from './home.module.css';

const CLIENTS = [
  {name: 'AspNetApi', delay: 900},
  {name: 'OrdersService', delay: 1100},
  {name: 'ProductService', delay: 800},
  {name: 'UserService', delay: 1200},
];

export default function LivePropagation() {
  const reduced = usePrefersReducedMotion();
  const [phase, setPhase] = useState(-1);
  const played = useRef(false);
  const sectionRef = useRef(null);

  const run = () => {
    if (reduced) {
      setPhase(CLIENTS.length);
      return;
    }
    setPhase(0);
  };

  useEffect(() => {
    if (reduced) {
      setPhase(CLIENTS.length);
    }
  }, [reduced]);

  useEffect(() => {
    if (reduced || played.current) {
      return undefined;
    }
    const node = sectionRef.current;
    if (!node) {
      return undefined;
    }
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !played.current) {
          played.current = true;
          setPhase(0);
          observer.disconnect();
        }
      },
      {threshold: 0.35},
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, [reduced]);

  useEffect(() => {
    if (phase < 0 || phase >= CLIENTS.length || reduced) {
      return undefined;
    }
    const timer = window.setTimeout(() => setPhase(phase + 1), CLIENTS[phase].delay);
    return () => window.clearTimeout(timer);
  }, [phase, reduced]);

  return (
    <section className={styles.band} ref={sectionRef}>
      <div className={clsx(styles.bandCopy, styles.bandCopyWide)}>
        <p className={styles.eyebrow}>Live reload</p>
        <h2 className={styles.heading}>
          Save once. Clients pick it up on their next poll.
        </h2>
        <p className={clsx(styles.lead, styles.leadSingle)}>
          No restart, no redeploy. Fig Web shows which sessions are current while they catch up.
        </p>
      </div>
      <div className={styles.propagate}>
        <div className={styles.settingCard}>
          <span className={styles.settingLabel}>MinLogLevel</span>
          <span className={styles.settingValue}>Information</span>
          <button type="button" className={styles.saveBtn} onClick={run}>
            Save
          </button>
        </div>
        <div className={styles.fan}>
          {CLIENTS.map((client, index) => {
            const state =
              phase > index ? 'applied' : phase === index ? 'applying' : 'idle';
            return (
              <article
                key={client.name}
                className={clsx(styles.clientTile, styles[`client_${state}`])}>
                <header>
                  <span className={styles.clientName}>{client.name}</span>
                  <span className={styles.clientHealth}>Healthy</span>
                </header>
                <p className={styles.clientStatus}>
                  {state === 'applied' && 'Up to date'}
                  {state === 'applying' && 'Reloading…'}
                  {state === 'idle' && 'Waiting for poll'}
                </p>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
}
