import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docsSidebar: [
    {
      type: 'category',
      label: 'Getting Started',
      link: { type: 'generated-index' },
      items: [
        'getting-started/overview',
        'getting-started/installation',
        'getting-started/quick-start',
        'getting-started/configuration',
        'getting-started/dependency-injection',
      ],
    },
    {
      type: 'category',
      label: 'Core Concepts',
      link: { type: 'generated-index' },
      items: [
        'core-concepts/fluent-configuration',
        'core-concepts/guards',
        'core-concepts/guid-generation',
        'core-concepts/system-time',
        'core-concepts/execution-results',
      ],
    },
    {
      type: 'category',
      label: 'Domain-Driven Design',
      link: { type: 'generated-index' },
      items: [
        'domain-driven-design/entities-aggregates',
        'domain-driven-design/domain-events',
        'domain-driven-design/value-objects',
        'domain-driven-design/auditing',
        'domain-driven-design/soft-delete',
      ],
    },
    {
      type: 'category',
      label: 'Persistence',
      link: { type: 'generated-index' },
      items: [
        'persistence/repository-pattern',
        'persistence/specifications',
        'persistence/unit-of-work',
        'persistence/efcore',
        'persistence/dapper',
        'persistence/linq2db',
        'persistence/caching-memory',
        'persistence/caching-redis',
      ],
    },
    {
      type: 'category',
      label: 'CQRS & Mediator',
      link: { type: 'generated-index' },
      items: [
        'cqrs-mediator/command-query-bus',
        'cqrs-mediator/commands-handlers',
        'cqrs-mediator/queries-handlers',
        'cqrs-mediator/mediatr',
        'cqrs-mediator/wolverine',
      ],
    },
    {
      type: 'category',
      label: 'Event Handling',
      link: { type: 'generated-index' },
      items: [
        'event-handling/overview',
        'event-handling/in-memory',
        'event-handling/distributed',
        'event-handling/transactional-outbox',
        'event-handling/mediatr',
        'event-handling/masstransit',
        'event-handling/wolverine',
      ],
    },
    {
      type: 'category',
      label: 'Messaging',
      link: { type: 'generated-index' },
      items: [
        'messaging/overview',
        'messaging/transactional-outbox',
        'messaging/state-machines',
        'messaging/masstransit',
        'messaging/wolverine',
      ],
    },
    {
      type: 'category',
      label: 'State Machines',
      link: { type: 'generated-index' },
      items: [
        'state-machines/overview',
        'state-machines/stateless',
      ],
    },
    {
      type: 'category',
      label: 'Caching',
      link: { type: 'generated-index' },
      items: [
        'caching/overview',
        'caching/memory',
        'caching/redis',
      ],
    },
    {
      type: 'category',
      label: 'Blob Storage',
      link: { type: 'generated-index' },
      items: [
        'blob-storage/overview',
        'blob-storage/azure',
        'blob-storage/s3',
      ],
    },
    {
      type: 'category',
      label: 'Serialization',
      link: { type: 'generated-index' },
      items: [
        'serialization/overview',
        'serialization/newtonsoft',
        'serialization/system-text-json',
      ],
    },
    {
      type: 'category',
      label: 'Validation',
      link: { type: 'generated-index' },
      items: [
        'validation/fluent-validation',
      ],
    },
    {
      type: 'category',
      label: 'Email',
      link: { type: 'generated-index' },
      items: [
        'email/overview',
        'email/sendgrid',
      ],
    },
    {
      type: 'category',
      label: 'Multi-Tenancy',
      link: { type: 'generated-index' },
      items: [
        'multi-tenancy/overview',
        'multi-tenancy/finbuckle',
      ],
    },
    {
      type: 'category',
      label: 'Security & Web',
      link: { type: 'generated-index' },
      items: [
        'security-web/authorization',
        'security-web/web-utilities',
      ],
    },
    {
      type: 'category',
      label: 'Architecture Guides',
      link: { type: 'generated-index' },
      items: [
        'architecture-guides/clean-architecture',
        'architecture-guides/microservices',
        'architecture-guides/event-driven',
      ],
    },
    {
      type: 'category',
      label: 'Examples & Recipes',
      link: { type: 'generated-index' },
      items: [
        'examples-recipes/hr-leave-management',
        'examples-recipes/event-handling',
        'examples-recipes/caching',
        'examples-recipes/messaging',
      ],
    },
    {
      type: 'category',
      label: 'Testing',
      link: { type: 'generated-index' },
      items: [
        'testing/overview',
        'testing/test-base-classes',
      ],
    },
    {
      type: 'category',
      label: 'API Reference',
      link: { type: 'generated-index' },
      items: [
        'api-reference/nuget-packages',
        'api-reference/changelog',
        'api-reference/migration-guide',
      ],
    },
  ],
};

export default sidebars;
