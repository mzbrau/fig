import React, {useState} from 'react';
import clsx from 'clsx';
import useBaseUrl from '@docusaurus/useBaseUrl';
import styles from './home.module.css';

const PROPERTIES = [
  {
    id: 'connection',
    region: 'database',
    lines: [
      '[Setting("Primary database connection")]',
      '[Category(Category.Database)]',
      '[Secret]',
      'public string ConnectionString { get; set; }',
    ],
  },
  {
    id: 'log',
    region: 'logging',
    lines: [
      '[Setting("Minimum log level")]',
      '[Category(Category.Logging)]',
      '[ValidValues(typeof(LogEventLevel))]',
      'public LogEventLevel MinLogLevel { get; set; }',
    ],
  },
  {
    id: 'api',
    region: 'api',
    lines: [
      '[Setting("External API base URL")]',
      '[Validation(ValidationType.NotEmpty)]',
      'public string ExternalApiUrl { get; set; }',
    ],
  },
  {
    id: 'timeout',
    region: 'timeout',
    lines: [
      '[Setting("API timeout in seconds")]',
      '[Validation(ValidationType.GreaterThanZero)]',
      'public int ApiTimeoutSeconds { get; set; } = 30;',
    ],
  },
];

export default function CodeToUi() {
  const [active, setActive] = useState(null);
  const editors = useBaseUrl('/img/landing-page/setting-editors.png');

  return (
    <section className={styles.band}>
      <div className={styles.bandCopy}>
        <p className={styles.eyebrow}>Integration</p>
        <h2 className={styles.heading}>Your class is the schema.</h2>
        <p className={styles.lead}>
          Attributes decide the UI: secrets stay masked, enums become
          dropdowns, and validation runs before save. Hover a property to
          see the matching control.
        </p>
      </div>
      <div className={styles.split}>
        <pre className={styles.codePane}>
          <code>
            <span className={styles.kw}>using</span>
            {' Fig.Client.Abstractions.Attributes;\n'}
            <span className={styles.kw}>using</span>
            {' Fig.Client.Abstractions.Data;\n'}
            <span className={styles.kw}>using</span>
            {' Fig.Client.Abstractions.Validation;\n\n'}
            <span className={styles.kw}>public class</span>
            {' Settings : SettingsBase\n{\n'}
            {PROPERTIES.map((property) => (
              <button
                key={property.id}
                type="button"
                className={clsx(
                  styles.codeProp,
                  active === property.id && styles.codePropActive,
                )}
                onMouseEnter={() => setActive(property.id)}
                onMouseLeave={() => setActive(null)}
                onFocus={() => setActive(property.id)}
                onBlur={() => setActive(null)}>
                {property.lines.map((line) => (
                  <span key={line}>{`    ${line}\n`}</span>
                ))}
              </button>
            ))}
            {'}'}
          </code>
        </pre>
        <div className={styles.editorPane}>
          <img
            src={editors}
            alt="Fig setting editors generated from the typed settings class"
          />
          {PROPERTIES.map((property) => (
            <span
              key={property.id}
              className={clsx(
                styles.hotspot,
                styles[`hotspot_${property.region}`],
                active === property.id && styles.hotspotOn,
              )}
              aria-hidden="true"
            />
          ))}
        </div>
      </div>
    </section>
  );
}
