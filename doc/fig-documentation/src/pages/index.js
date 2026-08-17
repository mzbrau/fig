import React from 'react';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Atmosphere from '../components/Home/Atmosphere';
import AnnouncementBanner from '../components/Home/AnnouncementBanner';
import Hero from '../components/Home/Hero';
import ProductTheater from '../components/Home/ProductTheater';
import CodeToUi from '../components/Home/CodeToUi';
import LivePropagation from '../components/Home/LivePropagation';
import QualityBand from '../components/Home/QualityBand';
import ProofStrip from '../components/Home/ProofStrip';
import FinalCta from '../components/Home/FinalCta';
import styles from '../components/Home/home.module.css';

export default function Home() {
  const {siteConfig} = useDocusaurusContext();

  return (
    <Layout
      title={siteConfig.title}
      description="Centralized settings management for dotnet microservices">
      <div className={styles.page}>
        <Atmosphere />
        <AnnouncementBanner />
        <Hero>
          <ProductTheater />
        </Hero>
        <main>
          <CodeToUi />
          <LivePropagation />
          <QualityBand />
          <ProofStrip />
          <FinalCta />
        </main>
      </div>
    </Layout>
  );
}
