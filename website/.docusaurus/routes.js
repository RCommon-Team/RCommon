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
    component: ComponentCreator('/docs', '5da'),
    routes: [
      {
        path: '/docs/2.4.1',
        component: ComponentCreator('/docs/2.4.1', '86f'),
        routes: [
          {
            path: '/docs/2.4.1',
            component: ComponentCreator('/docs/2.4.1', '37d'),
            routes: [
              {
                path: '/docs/2.4.1',
                component: ComponentCreator('/docs/2.4.1', '3b8'),
                exact: true
              },
              {
                path: '/docs/2.4.1/api-reference/changelog',
                component: ComponentCreator('/docs/2.4.1/api-reference/changelog', '1bd'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/api-reference/migration-guide',
                component: ComponentCreator('/docs/2.4.1/api-reference/migration-guide', '297'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/api-reference/nuget-packages',
                component: ComponentCreator('/docs/2.4.1/api-reference/nuget-packages', '336'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/architecture-guides/clean-architecture',
                component: ComponentCreator('/docs/2.4.1/architecture-guides/clean-architecture', 'f1e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/architecture-guides/event-driven',
                component: ComponentCreator('/docs/2.4.1/architecture-guides/event-driven', '972'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/architecture-guides/microservices',
                component: ComponentCreator('/docs/2.4.1/architecture-guides/microservices', 'a57'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/blob-storage/azure',
                component: ComponentCreator('/docs/2.4.1/blob-storage/azure', 'b8d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/blob-storage/overview',
                component: ComponentCreator('/docs/2.4.1/blob-storage/overview', '6d4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/blob-storage/s3',
                component: ComponentCreator('/docs/2.4.1/blob-storage/s3', '7a1'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/caching/memory',
                component: ComponentCreator('/docs/2.4.1/caching/memory', 'd09'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/caching/overview',
                component: ComponentCreator('/docs/2.4.1/caching/overview', '1cc'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/caching/redis',
                component: ComponentCreator('/docs/2.4.1/caching/redis', '91c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/api-reference',
                component: ComponentCreator('/docs/2.4.1/category/api-reference', 'bda'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/architecture-guides',
                component: ComponentCreator('/docs/2.4.1/category/architecture-guides', '17d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/blob-storage',
                component: ComponentCreator('/docs/2.4.1/category/blob-storage', 'eca'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/caching',
                component: ComponentCreator('/docs/2.4.1/category/caching', 'b54'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/core-concepts',
                component: ComponentCreator('/docs/2.4.1/category/core-concepts', 'eeb'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/cqrs--mediator',
                component: ComponentCreator('/docs/2.4.1/category/cqrs--mediator', '363'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/domain-driven-design',
                component: ComponentCreator('/docs/2.4.1/category/domain-driven-design', '37c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/email',
                component: ComponentCreator('/docs/2.4.1/category/email', 'ddc'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/event-handling',
                component: ComponentCreator('/docs/2.4.1/category/event-handling', '689'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/examples--recipes',
                component: ComponentCreator('/docs/2.4.1/category/examples--recipes', '62b'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/getting-started',
                component: ComponentCreator('/docs/2.4.1/category/getting-started', '339'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/messaging',
                component: ComponentCreator('/docs/2.4.1/category/messaging', '2df'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/multi-tenancy',
                component: ComponentCreator('/docs/2.4.1/category/multi-tenancy', '9ee'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/persistence',
                component: ComponentCreator('/docs/2.4.1/category/persistence', '774'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/security--web',
                component: ComponentCreator('/docs/2.4.1/category/security--web', '01f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/serialization',
                component: ComponentCreator('/docs/2.4.1/category/serialization', 'dfc'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/state-machines',
                component: ComponentCreator('/docs/2.4.1/category/state-machines', '7c7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/testing',
                component: ComponentCreator('/docs/2.4.1/category/testing', 'd69'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/category/validation',
                component: ComponentCreator('/docs/2.4.1/category/validation', '4bf'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/core-concepts/execution-results',
                component: ComponentCreator('/docs/2.4.1/core-concepts/execution-results', '559'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/core-concepts/fluent-configuration',
                component: ComponentCreator('/docs/2.4.1/core-concepts/fluent-configuration', 'd7c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/core-concepts/guards',
                component: ComponentCreator('/docs/2.4.1/core-concepts/guards', 'd0f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/core-concepts/guid-generation',
                component: ComponentCreator('/docs/2.4.1/core-concepts/guid-generation', 'd9c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/core-concepts/system-time',
                component: ComponentCreator('/docs/2.4.1/core-concepts/system-time', 'fff'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/cqrs-mediator/command-query-bus',
                component: ComponentCreator('/docs/2.4.1/cqrs-mediator/command-query-bus', '192'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/cqrs-mediator/commands-handlers',
                component: ComponentCreator('/docs/2.4.1/cqrs-mediator/commands-handlers', 'f5f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/cqrs-mediator/mediatr',
                component: ComponentCreator('/docs/2.4.1/cqrs-mediator/mediatr', '91f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/cqrs-mediator/queries-handlers',
                component: ComponentCreator('/docs/2.4.1/cqrs-mediator/queries-handlers', '3b7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/cqrs-mediator/wolverine',
                component: ComponentCreator('/docs/2.4.1/cqrs-mediator/wolverine', '089'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/domain-driven-design/auditing',
                component: ComponentCreator('/docs/2.4.1/domain-driven-design/auditing', 'cbb'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/domain-driven-design/domain-events',
                component: ComponentCreator('/docs/2.4.1/domain-driven-design/domain-events', 'c9f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/domain-driven-design/entities-aggregates',
                component: ComponentCreator('/docs/2.4.1/domain-driven-design/entities-aggregates', '086'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/domain-driven-design/soft-delete',
                component: ComponentCreator('/docs/2.4.1/domain-driven-design/soft-delete', 'de7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/domain-driven-design/value-objects',
                component: ComponentCreator('/docs/2.4.1/domain-driven-design/value-objects', '635'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/email/overview',
                component: ComponentCreator('/docs/2.4.1/email/overview', 'a99'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/email/sendgrid',
                component: ComponentCreator('/docs/2.4.1/email/sendgrid', '98d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/event-handling/distributed',
                component: ComponentCreator('/docs/2.4.1/event-handling/distributed', '52b'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/event-handling/in-memory',
                component: ComponentCreator('/docs/2.4.1/event-handling/in-memory', 'efa'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/event-handling/masstransit',
                component: ComponentCreator('/docs/2.4.1/event-handling/masstransit', '5c2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/event-handling/mediatr',
                component: ComponentCreator('/docs/2.4.1/event-handling/mediatr', 'e13'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/event-handling/overview',
                component: ComponentCreator('/docs/2.4.1/event-handling/overview', '4aa'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/event-handling/transactional-outbox',
                component: ComponentCreator('/docs/2.4.1/event-handling/transactional-outbox', '3e6'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/event-handling/wolverine',
                component: ComponentCreator('/docs/2.4.1/event-handling/wolverine', '30a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/examples-recipes/caching',
                component: ComponentCreator('/docs/2.4.1/examples-recipes/caching', 'd1a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/examples-recipes/event-handling',
                component: ComponentCreator('/docs/2.4.1/examples-recipes/event-handling', 'aa7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/examples-recipes/hr-leave-management',
                component: ComponentCreator('/docs/2.4.1/examples-recipes/hr-leave-management', 'eb2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/examples-recipes/messaging',
                component: ComponentCreator('/docs/2.4.1/examples-recipes/messaging', '0ef'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/getting-started/configuration',
                component: ComponentCreator('/docs/2.4.1/getting-started/configuration', 'a30'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/getting-started/dependency-injection',
                component: ComponentCreator('/docs/2.4.1/getting-started/dependency-injection', '5e4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/getting-started/installation',
                component: ComponentCreator('/docs/2.4.1/getting-started/installation', '8f4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/getting-started/overview',
                component: ComponentCreator('/docs/2.4.1/getting-started/overview', '925'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/getting-started/quick-start',
                component: ComponentCreator('/docs/2.4.1/getting-started/quick-start', '1d4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/messaging/masstransit',
                component: ComponentCreator('/docs/2.4.1/messaging/masstransit', '746'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/messaging/overview',
                component: ComponentCreator('/docs/2.4.1/messaging/overview', '526'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/messaging/state-machines',
                component: ComponentCreator('/docs/2.4.1/messaging/state-machines', '5aa'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/messaging/transactional-outbox',
                component: ComponentCreator('/docs/2.4.1/messaging/transactional-outbox', '190'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/messaging/wolverine',
                component: ComponentCreator('/docs/2.4.1/messaging/wolverine', 'b04'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/multi-tenancy/finbuckle',
                component: ComponentCreator('/docs/2.4.1/multi-tenancy/finbuckle', 'adb'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/multi-tenancy/overview',
                component: ComponentCreator('/docs/2.4.1/multi-tenancy/overview', '0ec'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/persistence/caching-memory',
                component: ComponentCreator('/docs/2.4.1/persistence/caching-memory', '336'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/persistence/caching-redis',
                component: ComponentCreator('/docs/2.4.1/persistence/caching-redis', '91f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/persistence/dapper',
                component: ComponentCreator('/docs/2.4.1/persistence/dapper', '69d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/persistence/efcore',
                component: ComponentCreator('/docs/2.4.1/persistence/efcore', 'c84'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/persistence/linq2db',
                component: ComponentCreator('/docs/2.4.1/persistence/linq2db', '9e0'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/persistence/repository-pattern',
                component: ComponentCreator('/docs/2.4.1/persistence/repository-pattern', '815'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/persistence/sagas',
                component: ComponentCreator('/docs/2.4.1/persistence/sagas', 'a34'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/persistence/specifications',
                component: ComponentCreator('/docs/2.4.1/persistence/specifications', '485'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/persistence/unit-of-work',
                component: ComponentCreator('/docs/2.4.1/persistence/unit-of-work', '0e5'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/security-web/authorization',
                component: ComponentCreator('/docs/2.4.1/security-web/authorization', '7c7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/security-web/web-utilities',
                component: ComponentCreator('/docs/2.4.1/security-web/web-utilities', '861'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/serialization/newtonsoft',
                component: ComponentCreator('/docs/2.4.1/serialization/newtonsoft', '5a0'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/serialization/overview',
                component: ComponentCreator('/docs/2.4.1/serialization/overview', '7d4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/serialization/system-text-json',
                component: ComponentCreator('/docs/2.4.1/serialization/system-text-json', '749'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/state-machines/overview',
                component: ComponentCreator('/docs/2.4.1/state-machines/overview', '957'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/state-machines/stateless',
                component: ComponentCreator('/docs/2.4.1/state-machines/stateless', '5b1'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/testing/overview',
                component: ComponentCreator('/docs/2.4.1/testing/overview', '8f9'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/testing/test-base-classes',
                component: ComponentCreator('/docs/2.4.1/testing/test-base-classes', 'eaa'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/2.4.1/validation/fluent-validation',
                component: ComponentCreator('/docs/2.4.1/validation/fluent-validation', '090'),
                exact: true,
                sidebar: "docsSidebar"
              }
            ]
          }
        ]
      },
      {
        path: '/docs/next',
        component: ComponentCreator('/docs/next', '9e8'),
        routes: [
          {
            path: '/docs/next',
            component: ComponentCreator('/docs/next', '60d'),
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
                path: '/docs/next/core-concepts/modular-composition',
                component: ComponentCreator('/docs/next/core-concepts/modular-composition', '40a'),
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
                path: '/docs/next/event-handling/outbox-producer-processor-topology',
                component: ComponentCreator('/docs/next/event-handling/outbox-producer-processor-topology', 'fbb'),
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
                path: '/docs/next/examples-recipes/domain-driven-design',
                component: ComponentCreator('/docs/next/examples-recipes/domain-driven-design', '4e0'),
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
                path: '/docs/next/persistence/aggregate-repository',
                component: ComponentCreator('/docs/next/persistence/aggregate-repository', '83b'),
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
        component: ComponentCreator('/docs', 'e7a'),
        routes: [
          {
            path: '/docs',
            component: ComponentCreator('/docs', '3b2'),
            routes: [
              {
                path: '/docs',
                component: ComponentCreator('/docs', '20e'),
                exact: true
              },
              {
                path: '/docs/api-reference/changelog',
                component: ComponentCreator('/docs/api-reference/changelog', '565'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/api-reference/migration-guide',
                component: ComponentCreator('/docs/api-reference/migration-guide', '15d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/api-reference/nuget-packages',
                component: ComponentCreator('/docs/api-reference/nuget-packages', '75c'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/architecture-guides/clean-architecture',
                component: ComponentCreator('/docs/architecture-guides/clean-architecture', '142'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/architecture-guides/event-driven',
                component: ComponentCreator('/docs/architecture-guides/event-driven', '6e9'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/architecture-guides/microservices',
                component: ComponentCreator('/docs/architecture-guides/microservices', '7c5'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/blob-storage/azure',
                component: ComponentCreator('/docs/blob-storage/azure', '871'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/blob-storage/overview',
                component: ComponentCreator('/docs/blob-storage/overview', 'c02'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/blob-storage/s3',
                component: ComponentCreator('/docs/blob-storage/s3', 'b3a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/caching/memory',
                component: ComponentCreator('/docs/caching/memory', '005'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/caching/overview',
                component: ComponentCreator('/docs/caching/overview', 'a23'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/caching/redis',
                component: ComponentCreator('/docs/caching/redis', '416'),
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
                component: ComponentCreator('/docs/core-concepts/execution-results', '900'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/fluent-configuration',
                component: ComponentCreator('/docs/core-concepts/fluent-configuration', '795'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/guards',
                component: ComponentCreator('/docs/core-concepts/guards', '276'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/guid-generation',
                component: ComponentCreator('/docs/core-concepts/guid-generation', '491'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/modular-composition',
                component: ComponentCreator('/docs/core-concepts/modular-composition', '509'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/core-concepts/system-time',
                component: ComponentCreator('/docs/core-concepts/system-time', 'a78'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/command-query-bus',
                component: ComponentCreator('/docs/cqrs-mediator/command-query-bus', 'b33'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/commands-handlers',
                component: ComponentCreator('/docs/cqrs-mediator/commands-handlers', 'eae'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/mediatr',
                component: ComponentCreator('/docs/cqrs-mediator/mediatr', '099'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/queries-handlers',
                component: ComponentCreator('/docs/cqrs-mediator/queries-handlers', 'be0'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/cqrs-mediator/wolverine',
                component: ComponentCreator('/docs/cqrs-mediator/wolverine', 'b6a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/auditing',
                component: ComponentCreator('/docs/domain-driven-design/auditing', 'ef5'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/domain-events',
                component: ComponentCreator('/docs/domain-driven-design/domain-events', 'e58'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/entities-aggregates',
                component: ComponentCreator('/docs/domain-driven-design/entities-aggregates', '521'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/soft-delete',
                component: ComponentCreator('/docs/domain-driven-design/soft-delete', '479'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/domain-driven-design/value-objects',
                component: ComponentCreator('/docs/domain-driven-design/value-objects', '159'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/email/overview',
                component: ComponentCreator('/docs/email/overview', 'e52'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/email/sendgrid',
                component: ComponentCreator('/docs/email/sendgrid', 'bdc'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/distributed',
                component: ComponentCreator('/docs/event-handling/distributed', 'a37'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/in-memory',
                component: ComponentCreator('/docs/event-handling/in-memory', 'f5b'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/masstransit',
                component: ComponentCreator('/docs/event-handling/masstransit', 'a58'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/mediatr',
                component: ComponentCreator('/docs/event-handling/mediatr', '74f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/outbox-producer-processor-topology',
                component: ComponentCreator('/docs/event-handling/outbox-producer-processor-topology', '1ba'),
                exact: true
              },
              {
                path: '/docs/event-handling/overview',
                component: ComponentCreator('/docs/event-handling/overview', 'de6'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/transactional-outbox',
                component: ComponentCreator('/docs/event-handling/transactional-outbox', '7cb'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/event-handling/wolverine',
                component: ComponentCreator('/docs/event-handling/wolverine', '81e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/examples-recipes/caching',
                component: ComponentCreator('/docs/examples-recipes/caching', 'ff5'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/examples-recipes/event-handling',
                component: ComponentCreator('/docs/examples-recipes/event-handling', '57f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/examples-recipes/hr-leave-management',
                component: ComponentCreator('/docs/examples-recipes/hr-leave-management', '1d2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/examples-recipes/messaging',
                component: ComponentCreator('/docs/examples-recipes/messaging', 'c04'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/configuration',
                component: ComponentCreator('/docs/getting-started/configuration', 'b9d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/dependency-injection',
                component: ComponentCreator('/docs/getting-started/dependency-injection', '6bf'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/installation',
                component: ComponentCreator('/docs/getting-started/installation', '713'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/overview',
                component: ComponentCreator('/docs/getting-started/overview', '388'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/getting-started/quick-start',
                component: ComponentCreator('/docs/getting-started/quick-start', '196'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/masstransit',
                component: ComponentCreator('/docs/messaging/masstransit', '9e4'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/overview',
                component: ComponentCreator('/docs/messaging/overview', '6f6'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/state-machines',
                component: ComponentCreator('/docs/messaging/state-machines', 'ef5'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/transactional-outbox',
                component: ComponentCreator('/docs/messaging/transactional-outbox', '855'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/messaging/wolverine',
                component: ComponentCreator('/docs/messaging/wolverine', '842'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/multi-tenancy/finbuckle',
                component: ComponentCreator('/docs/multi-tenancy/finbuckle', '9c2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/multi-tenancy/overview',
                component: ComponentCreator('/docs/multi-tenancy/overview', '4d2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/caching-memory',
                component: ComponentCreator('/docs/persistence/caching-memory', '750'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/caching-redis',
                component: ComponentCreator('/docs/persistence/caching-redis', 'b17'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/dapper',
                component: ComponentCreator('/docs/persistence/dapper', '738'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/efcore',
                component: ComponentCreator('/docs/persistence/efcore', 'bc2'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/linq2db',
                component: ComponentCreator('/docs/persistence/linq2db', 'fc9'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/repository-pattern',
                component: ComponentCreator('/docs/persistence/repository-pattern', '17e'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/sagas',
                component: ComponentCreator('/docs/persistence/sagas', '300'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/specifications',
                component: ComponentCreator('/docs/persistence/specifications', 'fef'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/persistence/unit-of-work',
                component: ComponentCreator('/docs/persistence/unit-of-work', '27a'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/security-web/authorization',
                component: ComponentCreator('/docs/security-web/authorization', '906'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/security-web/web-utilities',
                component: ComponentCreator('/docs/security-web/web-utilities', '6c7'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/serialization/newtonsoft',
                component: ComponentCreator('/docs/serialization/newtonsoft', 'e2d'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/serialization/overview',
                component: ComponentCreator('/docs/serialization/overview', 'd60'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/serialization/system-text-json',
                component: ComponentCreator('/docs/serialization/system-text-json', 'b35'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/state-machines/overview',
                component: ComponentCreator('/docs/state-machines/overview', 'acf'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/state-machines/stateless',
                component: ComponentCreator('/docs/state-machines/stateless', '16f'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/testing/overview',
                component: ComponentCreator('/docs/testing/overview', 'c28'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/testing/test-base-classes',
                component: ComponentCreator('/docs/testing/test-base-classes', '729'),
                exact: true,
                sidebar: "docsSidebar"
              },
              {
                path: '/docs/validation/fluent-validation',
                component: ComponentCreator('/docs/validation/fluent-validation', 'fd8'),
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
