import React, { useEffect } from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';

import styles from './index.module.css';

const FeatureSection = ({ imageUrl, title, description, isReversed }) => {
  return (
    <div className={clsx(styles.featureSection, isReversed && styles.reversed)}>
      <div className={styles.featureContent}>
        <h2>{title}</h2>
        <p>{description}</p>
      </div>
      <div className={styles.featureImage}>
        <img src={imageUrl} alt={title} />
      </div>
    </div>
  );
};

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className={clsx('container', styles.parallaxContainer)}>
        <div className={styles.logoWrapper}>
          <img 
            src='img/fig_logo_name_right_orange_299x135.png'
            className={styles.parallaxLogo}
          />
        </div>
        <p className={clsx("hero__subtitle", styles.parallaxText)}>{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/intro">
            Get Started with Fig
          </Link>
        </div>
      </div>
    </header>
  );
}

export default function Home() {
  const {siteConfig} = useDocusaurusContext();

  useEffect(() => {
    const handleScroll = () => {
      const elements = document.querySelectorAll(`.${styles.featureSection}`);
      elements.forEach(element => {
        const rect = element.getBoundingClientRect();
        const isVisible = rect.top < window.innerHeight && rect.bottom >= 0;
        if (isVisible) {
          element.classList.add(styles.visible);
        }
      });
    };

    window.addEventListener('scroll', handleScroll);
    handleScroll(); // Initial check
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  return (
    <Layout
      title={`${siteConfig.title}`}
      description="Centralized settings management for dotnet microservices">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
        <div className={styles.featuresContainer}>
          <FeatureSection
            imageUrl="/img/feature1.gif"
            title="Easy Configuration Management"
            description="Manage all your microservice settings in one place with our intuitive interface"
          />
          <FeatureSection
            imageUrl="/img/feature2.gif"
            title="Real-time Updates"
            description="Changes propagate instantly across your infrastructure"
            isReversed
          />
          <FeatureSection
            imageUrl="/img/feature3.gif"
            title="Version Control"
            description="Track and roll back changes with built-in versioning"
          />
        </div>
      </main>
    </Layout>
  );
}
