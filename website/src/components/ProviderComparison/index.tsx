import React from 'react';
import styles from './styles.module.css';

interface Provider {
  name: string;
  packageName: string;
  features: Record<string, boolean | string>;
}

interface ProviderComparisonProps {
  title: string;
  features: string[];
  providers: Provider[];
}

export default function ProviderComparison({title, features, providers}: ProviderComparisonProps): JSX.Element {
  return (
    <div className={styles.container}>
      <h3 className={styles.title}>{title}</h3>
      <table className={styles.table}>
        <thead>
          <tr>
            <th>Feature</th>
            {providers.map((p) => (
              <th key={p.name}>{p.name}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {features.map((feature) => (
            <tr key={feature}>
              <td>{feature}</td>
              {providers.map((p) => (
                <td key={p.name} className={styles.featureCell}>
                  {typeof p.features[feature] === 'boolean'
                    ? p.features[feature] ? '✅' : '❌'
                    : p.features[feature] || '—'}
                </td>
              ))}
            </tr>
          ))}
          <tr className={styles.packageRow}>
            <td><strong>Package</strong></td>
            {providers.map((p) => (
              <td key={p.name}><code>{p.packageName}</code></td>
            ))}
          </tr>
        </tbody>
      </table>
    </div>
  );
}
