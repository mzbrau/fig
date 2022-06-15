import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

const FeatureList = [
  {
    title: 'Easy to Integrate',
    Svg: require('@site/static/img/undraw_docusaurus_mountain.svg').default,
    description: (
      <>
        Fig can be integrated into existing applications with just a few lines of code.
        Use the settings in your application just as any other class.
      </>
    ),
  },
  {
    title: 'Supports Different Property Types',
    Svg: require('@site/static/img/undraw_docusaurus_tree.svg').default,
    description: (
      <>
        Specialized editors for strings, integers, bools, data tables, 
        drop downs, secret settings and many more.
      </>
    ),
  },
  {
    title: 'Live Reload',
    Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
    description: (
      <>
        Updated values available to your application after they are applied in the web interface.
      </>
    ),
  },
  {
    title: 'Offline Settings',
    Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
    description: (
      <>
        Last loaded settings are available to clients even if the api is offline.
      </>
    ),
  },
  {
    title: 'Client Management',
    Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
    description: (
      <>
        See connected settings clients listed in real time and request restart of individual clients.
      </>
    ),
  },
  {
    title: 'Audit Logging',
    Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
    description: (
      <>
        All settings changes are logged and can be reviewed in the audit log.
      </>
    ),
  },
];

function Feature({Svg, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
