import React from 'react';
import ComponentCreator from '@docusaurus/ComponentCreator';

export default [
  {
    path: '/search',
    component: ComponentCreator('/search', '822'),
    exact: true
  },
  {
    path: '/docs',
    component: ComponentCreator('/docs', '62c'),
    routes: [
      {
        path: '/docs/next',
        component: ComponentCreator('/docs/next', 'f3e'),
        routes: [
          {
            path: '/docs/next',
            component: ComponentCreator('/docs/next', 'a23'),
            routes: [
              {
                path: '/docs/next',
                component: ComponentCreator('/docs/next', 'c40'),
                exact: true
              },
              {
                path: '/docs/next/api-reference/changelog',
                component: ComponentCreator('/docs/next/api-reference/changelog', 'b56'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/api-reference/migration-guide',
                component: ComponentCreator('/docs/next/api-reference/migration-guide', '142'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/api-reference/nuget-packages',
                component: ComponentCreator('/docs/next/api-reference/nuget-packages', '537'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/architecture-guides/clean-architecture',
                component: ComponentCreator('/docs/next/architecture-guides/clean-architecture', 'b73'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/architecture-guides/event-driven',
                component: ComponentCreator('/docs/next/architecture-guides/event-driven', 'd41'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/architecture-guides/microservices',
                component: ComponentCreator('/docs/next/architecture-guides/microservices', '317'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/blob-storage/azure',
                component: ComponentCreator('/docs/next/blob-storage/azure', '6c3'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/blob-storage/overview',
                component: ComponentCreator('/docs/next/blob-storage/overview', '4db'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/blob-storage/s3',
                component: ComponentCreator('/docs/next/blob-storage/s3', 'b75'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/caching/memory',
                component: ComponentCreator('/docs/next/caching/memory', 'a65'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/caching/overview',
                component: ComponentCreator('/docs/next/caching/overview', '43e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/caching/redis',
                component: ComponentCreator('/docs/next/caching/redis', 'a6d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/api-reference',
                component: ComponentCreator('/docs/next/category/api-reference', '824'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/architecture-guides',
                component: ComponentCreator('/docs/next/category/architecture-guides', '53e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/blob-storage',
                component: ComponentCreator('/docs/next/category/blob-storage', 'a18'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/caching',
                component: ComponentCreator('/docs/next/category/caching', '45f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/core-concepts',
                component: ComponentCreator('/docs/next/category/core-concepts', '206'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/cqrs--mediator',
                component: ComponentCreator('/docs/next/category/cqrs--mediator', '8f5'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/domain-driven-design',
                component: ComponentCreator('/docs/next/category/domain-driven-design', '91e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/email',
                component: ComponentCreator('/docs/next/category/email', 'b81'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/event-handling',
                component: ComponentCreator('/docs/next/category/event-handling', '6ae'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/examples--recipes',
                component: ComponentCreator('/docs/next/category/examples--recipes', '571'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/getting-started',
                component: ComponentCreator('/docs/next/category/getting-started', 'fac'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/messaging',
                component: ComponentCreator('/docs/next/category/messaging', 'ca4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/multi-tenancy',
                component: ComponentCreator('/docs/next/category/multi-tenancy', '302'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/persistence',
                component: ComponentCreator('/docs/next/category/persistence', 'b01'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/security--web',
                component: ComponentCreator('/docs/next/category/security--web', 'ef7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/serialization',
                component: ComponentCreator('/docs/next/category/serialization', '0fd'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/state-machines',
                component: ComponentCreator('/docs/next/category/state-machines', '87b'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/testing',
                component: ComponentCreator('/docs/next/category/testing', '49c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/category/validation',
                component: ComponentCreator('/docs/next/category/validation', '2c1'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/core-concepts/execution-results',
                component: ComponentCreator('/docs/next/core-concepts/execution-results', '78c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/core-concepts/fluent-configuration',
                component: ComponentCreator('/docs/next/core-concepts/fluent-configuration', 'f58'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/core-concepts/guards',
                component: ComponentCreator('/docs/next/core-concepts/guards', '153'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/core-concepts/guid-generation',
                component: ComponentCreator('/docs/next/core-concepts/guid-generation', 'df6'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/core-concepts/system-time',
                component: ComponentCreator('/docs/next/core-concepts/system-time', '1a0'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/cqrs-mediator/command-query-bus',
                component: ComponentCreator('/docs/next/cqrs-mediator/command-query-bus', '4f6'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/cqrs-mediator/commands-handlers',
                component: ComponentCreator('/docs/next/cqrs-mediator/commands-handlers', 'f87'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/cqrs-mediator/mediatr',
                component: ComponentCreator('/docs/next/cqrs-mediator/mediatr', '6d6'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/cqrs-mediator/queries-handlers',
                component: ComponentCreator('/docs/next/cqrs-mediator/queries-handlers', '4a0'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/cqrs-mediator/wolverine',
                component: ComponentCreator('/docs/next/cqrs-mediator/wolverine', '131'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/domain-driven-design/auditing',
                component: ComponentCreator('/docs/next/domain-driven-design/auditing', 'c52'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/domain-driven-design/domain-events',
                component: ComponentCreator('/docs/next/domain-driven-design/domain-events', 'bd4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/domain-driven-design/entities-aggregates',
                component: ComponentCreator('/docs/next/domain-driven-design/entities-aggregates', 'b9f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/domain-driven-design/soft-delete',
                component: ComponentCreator('/docs/next/domain-driven-design/soft-delete', '526'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/domain-driven-design/value-objects',
                component: ComponentCreator('/docs/next/domain-driven-design/value-objects', '9f9'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/email/overview',
                component: ComponentCreator('/docs/next/email/overview', '4ba'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/email/sendgrid',
                component: ComponentCreator('/docs/next/email/sendgrid', '25e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/event-handling/distributed',
                component: ComponentCreator('/docs/next/event-handling/distributed', '30d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/event-handling/in-memory',
                component: ComponentCreator('/docs/next/event-handling/in-memory', 'c7a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/event-handling/masstransit',
                component: ComponentCreator('/docs/next/event-handling/masstransit', 'bd2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/event-handling/mediatr',
                component: ComponentCreator('/docs/next/event-handling/mediatr', 'ebf'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/event-handling/overview',
                component: ComponentCreator('/docs/next/event-handling/overview', '4d3'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/event-handling/transactional-outbox',
                component: ComponentCreator('/docs/next/event-handling/transactional-outbox', '232'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/event-handling/wolverine',
                component: ComponentCreator('/docs/next/event-handling/wolverine', 'f43'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/examples-recipes/caching',
                component: ComponentCreator('/docs/next/examples-recipes/caching', 'a2e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/examples-recipes/event-handling',
                component: ComponentCreator('/docs/next/examples-recipes/event-handling', 'aaa'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/examples-recipes/hr-leave-management',
                component: ComponentCreator('/docs/next/examples-recipes/hr-leave-management', 'e54'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/examples-recipes/messaging',
                component: ComponentCreator('/docs/next/examples-recipes/messaging', '27f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/getting-started/configuration',
                component: ComponentCreator('/docs/next/getting-started/configuration', '890'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/getting-started/dependency-injection',
                component: ComponentCreator('/docs/next/getting-started/dependency-injection', '495'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/getting-started/installation',
                component: ComponentCreator('/docs/next/getting-started/installation', 'b7a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/getting-started/overview',
                component: ComponentCreator('/docs/next/getting-started/overview', '14c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/getting-started/quick-start',
                component: ComponentCreator('/docs/next/getting-started/quick-start', 'c8c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/messaging/masstransit',
                component: ComponentCreator('/docs/next/messaging/masstransit', '149'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/messaging/overview',
                component: ComponentCreator('/docs/next/messaging/overview', 'e5d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/messaging/state-machines',
                component: ComponentCreator('/docs/next/messaging/state-machines', '15c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/messaging/transactional-outbox',
                component: ComponentCreator('/docs/next/messaging/transactional-outbox', '1e6'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/messaging/wolverine',
                component: ComponentCreator('/docs/next/messaging/wolverine', '78a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/multi-tenancy/finbuckle',
                component: ComponentCreator('/docs/next/multi-tenancy/finbuckle', 'f1f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/multi-tenancy/overview',
                component: ComponentCreator('/docs/next/multi-tenancy/overview', '2b4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/persistence/caching-memory',
                component: ComponentCreator('/docs/next/persistence/caching-memory', '51c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/persistence/caching-redis',
                component: ComponentCreator('/docs/next/persistence/caching-redis', '1ea'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/persistence/dapper',
                component: ComponentCreator('/docs/next/persistence/dapper', 'd63'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/persistence/efcore',
                component: ComponentCreator('/docs/next/persistence/efcore', '744'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/persistence/linq2db',
                component: ComponentCreator('/docs/next/persistence/linq2db', '642'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/persistence/repository-pattern',
                component: ComponentCreator('/docs/next/persistence/repository-pattern', 'fae'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/persistence/sagas',
                component: ComponentCreator('/docs/next/persistence/sagas', 'ec2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/persistence/specifications',
                component: ComponentCreator('/docs/next/persistence/specifications', '92b'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/persistence/unit-of-work',
                component: ComponentCreator('/docs/next/persistence/unit-of-work', '6e0'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/security-web/authorization',
                component: ComponentCreator('/docs/next/security-web/authorization', '7ca'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/security-web/web-utilities',
                component: ComponentCreator('/docs/next/security-web/web-utilities', '3d7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/serialization/newtonsoft',
                component: ComponentCreator('/docs/next/serialization/newtonsoft', 'b66'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/serialization/overview',
                component: ComponentCreator('/docs/next/serialization/overview', '820'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/serialization/system-text-json',
                component: ComponentCreator('/docs/next/serialization/system-text-json', '16a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/state-machines/overview',
                component: ComponentCreator('/docs/next/state-machines/overview', '6b6'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/state-machines/stateless',
                component: ComponentCreator('/docs/next/state-machines/stateless', '34e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/testing/overview',
                component: ComponentCreator('/docs/next/testing/overview', '75c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/testing/test-base-classes',
                component: ComponentCreator('/docs/next/testing/test-base-classes', '947'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/next/validation/fluent-validation',
                component: ComponentCreator('/docs/next/validation/fluent-validation', '195'),
                exact: true,
                sidebar: "docsSidebar"
              }
            ]
          }
        ]
      },
      {
        path: '/docs',
        component: ComponentCreator('/docs', '06f'),
        routes: [
          {
            path: '/docs',
            component: ComponentCreator('/docs', 'b2c'),
            routes: [
              {
                path: '/docs',
                component: ComponentCreator('/docs', '71c'),
                exact: true
              },
              {
                path: '/docs/api-reference/changelog',
                component: ComponentCreator('/docs/api-reference/changelog', 'f90'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/api-reference/migration-guide',
                component: ComponentCreator('/docs/api-reference/migration-guide', 'e22'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/api-reference/nuget-packages',
                component: ComponentCreator('/docs/api-reference/nuget-packages', '922'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/architecture-guides/clean-architecture',
                component: ComponentCreator('/docs/architecture-guides/clean-architecture', '393'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/architecture-guides/event-driven',
                component: ComponentCreator('/docs/architecture-guides/event-driven', '4d7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/architecture-guides/microservices',
                component: ComponentCreator('/docs/architecture-guides/microservices', 'f82'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/blob-storage/azure',
                component: ComponentCreator('/docs/blob-storage/azure', '371'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/blob-storage/overview',
                component: ComponentCreator('/docs/blob-storage/overview', 'c52'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/blob-storage/s3',
                component: ComponentCreator('/docs/blob-storage/s3', '2a4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/caching/memory',
                component: ComponentCreator('/docs/caching/memory', '04d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/caching/overview',
                component: ComponentCreator('/docs/caching/overview', '06f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/caching/redis',
                component: ComponentCreator('/docs/caching/redis', 'f95'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/api-reference',
                component: ComponentCreator('/docs/category/api-reference', '789'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/architecture-guides',
                component: ComponentCreator('/docs/category/architecture-guides', 'aa9'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/blob-storage',
                component: ComponentCreator('/docs/category/blob-storage', '7bc'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/caching',
                component: ComponentCreator('/docs/category/caching', 'fc8'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/core-concepts',
                component: ComponentCreator('/docs/category/core-concepts', '270'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/cqrs--mediator',
                component: ComponentCreator('/docs/category/cqrs--mediator', '7b5'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/domain-driven-design',
                component: ComponentCreator('/docs/category/domain-driven-design', '553'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/email',
                component: ComponentCreator('/docs/category/email', '497'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/event-handling',
                component: ComponentCreator('/docs/category/event-handling', 'beb'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/examples--recipes',
                component: ComponentCreator('/docs/category/examples--recipes', '246'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/getting-started',
                component: ComponentCreator('/docs/category/getting-started', 'd48'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/messaging',
                component: ComponentCreator('/docs/category/messaging', 'd87'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/multi-tenancy',
                component: ComponentCreator('/docs/category/multi-tenancy', '4aa'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/persistence',
                component: ComponentCreator('/docs/category/persistence', 'd6c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/security--web',
                component: ComponentCreator('/docs/category/security--web', '392'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/serialization',
                component: ComponentCreator('/docs/category/serialization', 'baf'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/state-machines',
                component: ComponentCreator('/docs/category/state-machines', '4e4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/testing',
                component: ComponentCreator('/docs/category/testing', '528'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/category/validation',
                component: ComponentCreator('/docs/category/validation', 'a2e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/execution-results',
                component: ComponentCreator('/docs/core-concepts/execution-results', '762'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/fluent-configuration',
                component: ComponentCreator('/docs/core-concepts/fluent-configuration', 'c31'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/guards',
                component: ComponentCreator('/docs/core-concepts/guards', '75e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/guid-generation',
                component: ComponentCreator('/docs/core-concepts/guid-generation', '1f1'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/system-time',
                component: ComponentCreator('/docs/core-concepts/system-time', '4cf'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/command-query-bus',
                component: ComponentCreator('/docs/cqrs-mediator/command-query-bus', 'bc1'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/commands-handlers',
                component: ComponentCreator('/docs/cqrs-mediator/commands-handlers', '083'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/mediatr',
                component: ComponentCreator('/docs/cqrs-mediator/mediatr', 'e5f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/queries-handlers',
                component: ComponentCreator('/docs/cqrs-mediator/queries-handlers', 'af8'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/wolverine',
                component: ComponentCreator('/docs/cqrs-mediator/wolverine', 'cad'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/auditing',
                component: ComponentCreator('/docs/domain-driven-design/auditing', '07c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/domain-events',
                component: ComponentCreator('/docs/domain-driven-design/domain-events', '083'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/entities-aggregates',
                component: ComponentCreator('/docs/domain-driven-design/entities-aggregates', '1b5'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/soft-delete',
                component: ComponentCreator('/docs/domain-driven-design/soft-delete', '3d7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/value-objects',
                component: ComponentCreator('/docs/domain-driven-design/value-objects', '2a9'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/email/overview',
                component: ComponentCreator('/docs/email/overview', '1da'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/email/sendgrid',
                component: ComponentCreator('/docs/email/sendgrid', 'c9e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/distributed',
                component: ComponentCreator('/docs/event-handling/distributed', '077'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/in-memory',
                component: ComponentCreator('/docs/event-handling/in-memory', '2d2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/masstransit',
                component: ComponentCreator('/docs/event-handling/masstransit', 'f04'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/mediatr',
                component: ComponentCreator('/docs/event-handling/mediatr', 'ff9'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/overview',
                component: ComponentCreator('/docs/event-handling/overview', '79d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/transactional-outbox',
                component: ComponentCreator('/docs/event-handling/transactional-outbox', 'dde'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/wolverine',
                component: ComponentCreator('/docs/event-handling/wolverine', '94e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/examples-recipes/caching',
                component: ComponentCreator('/docs/examples-recipes/caching', 'ea3'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/examples-recipes/event-handling',
                component: ComponentCreator('/docs/examples-recipes/event-handling', '5b6'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/examples-recipes/hr-leave-management',
                component: ComponentCreator('/docs/examples-recipes/hr-leave-management', '3bd'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/examples-recipes/messaging',
                component: ComponentCreator('/docs/examples-recipes/messaging', '63c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/configuration',
                component: ComponentCreator('/docs/getting-started/configuration', '91a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/dependency-injection',
                component: ComponentCreator('/docs/getting-started/dependency-injection', 'e67'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/installation',
                component: ComponentCreator('/docs/getting-started/installation', 'c31'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/overview',
                component: ComponentCreator('/docs/getting-started/overview', '4a5'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/quick-start',
                component: ComponentCreator('/docs/getting-started/quick-start', '099'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/masstransit',
                component: ComponentCreator('/docs/messaging/masstransit', '39e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/overview',
                component: ComponentCreator('/docs/messaging/overview', 'a4d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/state-machines',
                component: ComponentCreator('/docs/messaging/state-machines', '7fb'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/transactional-outbox',
                component: ComponentCreator('/docs/messaging/transactional-outbox', '4ed'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/wolverine',
                component: ComponentCreator('/docs/messaging/wolverine', '02a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/multi-tenancy/finbuckle',
                component: ComponentCreator('/docs/multi-tenancy/finbuckle', '00b'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/multi-tenancy/overview',
                component: ComponentCreator('/docs/multi-tenancy/overview', 'f0d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/caching-memory',
                component: ComponentCreator('/docs/persistence/caching-memory', '50a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/caching-redis',
                component: ComponentCreator('/docs/persistence/caching-redis', '3fb'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/dapper',
                component: ComponentCreator('/docs/persistence/dapper', '502'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/efcore',
                component: ComponentCreator('/docs/persistence/efcore', '24d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/linq2db',
                component: ComponentCreator('/docs/persistence/linq2db', '70e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/repository-pattern',
                component: ComponentCreator('/docs/persistence/repository-pattern', '47e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/sagas',
                component: ComponentCreator('/docs/persistence/sagas', '194'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/specifications',
                component: ComponentCreator('/docs/persistence/specifications', 'e4c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/unit-of-work',
                component: ComponentCreator('/docs/persistence/unit-of-work', '11a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/security-web/authorization',
                component: ComponentCreator('/docs/security-web/authorization', '8d4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/security-web/web-utilities',
                component: ComponentCreator('/docs/security-web/web-utilities', 'ea2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/serialization/newtonsoft',
                component: ComponentCreator('/docs/serialization/newtonsoft', '8ec'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/serialization/overview',
                component: ComponentCreator('/docs/serialization/overview', '740'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/serialization/system-text-json',
                component: ComponentCreator('/docs/serialization/system-text-json', '9c2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/state-machines/overview',
                component: ComponentCreator('/docs/state-machines/overview', 'f5e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/state-machines/stateless',
                component: ComponentCreator('/docs/state-machines/stateless', '1cc'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/testing/overview',
                component: ComponentCreator('/docs/testing/overview', '522'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/testing/test-base-classes',
                component: ComponentCreator('/docs/testing/test-base-classes', '1cd'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/validation/fluent-validation',
                component: ComponentCreator('/docs/validation/fluent-validation', '1bf'),
                exact: true,
                sidebar: "docsSidebar"
              }
            ]
          }
        ]
      }
    ]
  },
  {
    path: '/',
    component: ComponentCreator('/', 'e5f'),
    exact: true
  },
  {
    path: '*',
    component: ComponentCreator('*'),
  },
];
