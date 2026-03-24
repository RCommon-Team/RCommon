import React from 'react';
import clsx from 'clsx';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import styles from './index.module.css';

const features = [
  { icon: '🗄️', title: 'Persistence', description: 'Repository, Unit of Work, Specifications — with EF Core, Dapper, and Linq2Db providers', link: '/docs/persistence' },
  { icon: '⚡', title: 'CQRS & Mediator', description: 'Command/Query Bus with MediatR and Wolverine implementations', link: '/docs/cqrs-mediator' },
  { icon: '📡', title: 'Event Handling', description: 'In-memory and distributed events with transactional outbox support', link: '/docs/event-handling' },
  { icon: '📬', title: 'Messaging', description: 'Message bus with MassTransit and Wolverine, including state machines', link: '/docs/messaging' },
  { icon: '💾', title: 'Caching', description: 'Unified caching abstraction with Memory and Redis providers', link: '/docs/caching' },
  { icon: '🏗️', title: 'Domain-Driven Design', description: 'Entities, Aggregates, Domain Events, Auditing, and Soft Delete', link: '/docs/domain-driven-design' },
  { icon: '☁️', title: 'Blob Storage', description: 'Azure Blob Storage and Amazon S3 behind a unified abstraction', link: '/docs/blob-storage' },
  { icon: '🏢', title: 'Multi-Tenancy', description: 'Tenant resolution and isolation with Finbuckle integration', link: '/docs/multi-tenancy' },
  { icon: '✅', title: 'Validation & More', description: 'FluentValidation, Email (SMTP/SendGrid), JSON serialization, Security', link: '/docs/validation' },
];

const stats = [
  { value: '37+', label: 'NuGet Packages' },
  { value: '3', label: 'Target Frameworks' },
  { value: '20+', label: 'Working Examples' },
  { value: 'Apache 2.0', label: 'License' },
];

const architectures = [
  { icon: '🏛️', title: 'Clean Architecture', description: 'Layer your application with clear boundaries', link: '/docs/architecture-guides/clean-architecture' },
  { icon: '🔀', title: 'Microservices', description: 'Distributed systems with messaging and events', link: '/docs/architecture-guides/microservices' },
  { icon: '📡', title: 'Event-Driven', description: 'React to domain events across bounded contexts', link: '/docs/architecture-guides/event-driven' },
];

function HeroSection() {
  return (
    <div className={styles.hero}>
      <div className={styles.heroLabel}>Open Source .NET Infrastructure</div>
      <h1 className={styles.heroTitle}>
        Build Enterprise Applications<br />Without Reinventing the Wheel
      </h1>
      <p className={styles.heroSubtitle}>
        RCommon provides battle-tested abstractions for persistence, CQRS, event handling,
        messaging, caching, and more — so you can focus on your domain, not your plumbing.
      </p>
      <div className={styles.heroCta}>
        <Link className={clsx('button button--primary button--lg', styles.ctaPrimary)} to="/docs/getting-started">
          Get Started →
        </Link>
        <Link className={clsx('button button--outline button--lg', styles.ctaSecondary)} to="https://github.com/RCommon-Team/RCommon">
          View on GitHub
        </Link>
      </div>
      <div className={styles.heroInstall}>
        <code>dotnet add package RCommon.Core</code>
      </div>
    </div>
  );
}

function StatsSection() {
  return (
    <div className={styles.stats}>
      {stats.map((stat) => (
        <div key={stat.label} className={styles.stat}>
          <div className={styles.statValue}>{stat.value}</div>
          <div className={styles.statLabel}>{stat.label}</div>
        </div>
      ))}
    </div>
  );
}

function FeaturesSection() {
  return (
    <div className={styles.features}>
      <h2 className={styles.sectionTitle}>Everything You Need</h2>
      <p className={styles.sectionSubtitle}>Pluggable abstractions with multiple provider implementations</p>
      <div className={styles.featureGrid}>
        {features.map((feature) => (
          <Link key={feature.title} to={feature.link} className={styles.featureCard}>
            <div className={styles.featureIcon}>{feature.icon}</div>
            <h3>{feature.title}</h3>
            <p>{feature.description}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}

function CodeSection() {
  return (
    <div className={styles.codeSection}>
      <h2 className={styles.sectionTitle}>Simple, Fluent Configuration</h2>
      <p className={styles.sectionSubtitle}>Configure everything through a single fluent builder in Program.cs</p>
      <pre className={styles.codeBlock}>
        <code>{`builder
    .AddRCommon()
    .WithPersistence<EfCorePeristenceBuilder>(ef =>
        ef.AddDbContext<AppDbContext>(...))
    .WithMediator<MediatRBuilder>(mediator =>
        mediator.AddRequest<CreateOrderCommand>())
    .WithEventHandling<MassTransitBuilder>(events =>
        events.AddProducer<OrderCreatedEvent>())
    .WithCaching<MemoryCacheBuilder>()
    .WithValidation<FluentValidationBuilder>();`}</code>
      </pre>
    </div>
  );
}

function ArchitectureSection() {
  return (
    <div className={styles.architectures}>
      <h2 className={styles.sectionTitle}>Built for Any Architecture</h2>
      <p className={styles.sectionSubtitle}>RCommon fits into the architecture you choose</p>
      <div className={styles.archGrid}>
        {architectures.map((arch) => (
          <Link key={arch.title} to={arch.link} className={styles.archCard}>
            <div className={styles.archIcon}>{arch.icon}</div>
            <h3>{arch.title}</h3>
            <p>{arch.description}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}

export default function Home(): JSX.Element {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout title={siteConfig.title} description={siteConfig.tagline}>
      <main>
        <HeroSection />
        <StatsSection />
        <FeaturesSection />
        <CodeSection />
        <ArchitectureSection />
      </main>
    </Layout>
  );
}
