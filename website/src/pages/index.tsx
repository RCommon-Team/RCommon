import React from 'react';
import clsx from 'clsx';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import styles from './index.module.css';

function HomepageHeader() {
  const { siteConfig } = useDocusaurusContext();
  return (
    <header className={clsx('hero', styles.heroBanner)}>
      <div className="container">
        <div
          style={{
            display: `flex`,
            flexDirection: `column`,
            placeItems: `center`,
          }}
        >
          <h1 className="hero__title">{siteConfig.title}</h1>
          <p className="hero__subtitle">{siteConfig.tagline}</p>
        </div>
        <div className="flex justify-center flex-w">
          <Link className="button button--primary button--lg m-2" to="/docs">
            Documentation
          </Link>
          <Link
            className="button button--secondary button--lg m-2"
            href="https://github.com/RCommon-Team/RCommon"
          >
            GitHub
          </Link>
        </div>
      </div>
    </header>
  );
}

export default function Home(): JSX.Element {
  const { siteConfig } = useDocusaurusContext();
  return (
    <Layout
      title={siteConfig.title}
      description="RCommon is a .NET framework providing common abstractions for cross-cutting concerns including persistence, caching, messaging, and more."
    >
      <HomepageHeader />
      <main></main>
    </Layout>
  );
}
