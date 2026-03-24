import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

require('dotenv').config();

const DOCS_SERVER_ROOT_URLS = {
  development: 'localhost:8000',
  production: 'website-api.hasura.io',
  staging: 'website-api.stage.hasura.io',
};

const DOCS_SERVER_URLS = {
  development: `http://${DOCS_SERVER_ROOT_URLS.development}`,
  production: `https://${DOCS_SERVER_ROOT_URLS.production}/docs-services/docs-server`,
  staging: `https://${DOCS_SERVER_ROOT_URLS.staging}/docs-services/docs-server`,
};

const BOT_ROUTES = {
  development: `ws://${DOCS_SERVER_ROOT_URLS.development}/bot/query`,
  production: `wss://${DOCS_SERVER_ROOT_URLS.production}/docs-services/docs-server/bot/query`,
  staging: `wss://${DOCS_SERVER_ROOT_URLS.staging}/docs-services/docs-server/bot/query`,
};

const config: Config = {
  title: 'Hasura DDN Docs',
  tagline: 'Instant GraphQL on all your data',
  favicon: 'img/favicon.png',

  // Set the production url of your site here
  url: 'https://hasura.io',

  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: process.env.CF_PAGES === '1' ? '/' : '/docs/3.0',
  trailingSlash: true,
  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'hasura', // Usually your GitHub org/user name.
  projectName: 'docusaurus', // Usually your repo name.

  staticDirectories: ['static', 'public'],

  onBrokenLinks: 'throw',
  onBrokenAnchors: 'throw',
  onBrokenMarkdownLinks: 'throw',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },
  customFields: {
    docsBotEndpointURL: (() => {
      if (process.env.CF_PAGES === '1') {
        return BOT_ROUTES.staging; // if we're on CF pages, use the staging environment
      } else {
        const mode = process.env.release_mode;
        if (mode === 'staging') {
          return BOT_ROUTES.production; // use production route for staging
        }
        return BOT_ROUTES[mode ?? 'development'];
      }
    })(),
    docsServerURL: (() => {
      if (process.env.CF_PAGES === '1') {
        return DOCS_SERVER_URLS.staging; // if we're on CF pages, use the staging environment
      } else {
        const mode = process.env.release_mode;
        if (mode === 'staging') {
          return DOCS_SERVER_URLS.production; // use production route for staging
        }
        return DOCS_SERVER_URLS[mode ?? 'development'];
      }
    })(),
    hasuraVersion: 'ddn',
    DEV_TOKEN: process.env.DEV_TOKEN,
    openReplayIngestPoint: 'https://analytics-openreplay.hasura-app.io/ingest',
    openReplayProjectKey: 'x5WnKn7RdPjizi93Vp5I',
  },

  presets: [
    [
      'classic',
      {
        docs: {
          routeBasePath: '/',
          editUrl: ({ docPath }) => `https://github.com/hasura/ddn-docs/edit/main/docs/${docPath}`,
          breadcrumbs: true,
          // showLastUpdateAuthor: true,
          // showLastUpdateTime: true,
          lastVersion: 'current',
          versions: {
            current: {
              label: 'v3.x (DDN)',
              badge: true,
            },
          },
          sidebarCollapsible: true,
          sidebarPath: './sidebars.ts',
        },
        blog: false,
        googleTagManager: {
          containerId: 'GTM-PF5MQ2Z',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  plugins: [
    [
      '@docusaurus/plugin-content-docs',
      {
        id: 'wiki',
        path: 'wiki',
        routeBasePath: 'wiki',
        exclude: ['**/*.wip'],
        // sidebarPath: './sidebarsCommunity.js',
        // ... other options
      },
    ],
    async function tailwind(context, options) {
      return {
        name: 'docusaurus-tailwindcss',
        configurePostCss(postcssOptions) {
          // Appends TailwindCSS and AutoPrefixer.
          postcssOptions.plugins.push(require('tailwindcss'));
          postcssOptions.plugins.push(require('autoprefixer'));
          return postcssOptions;
        },
      };
    },
  ],
  themeConfig: {
    // Replace with your project's social card
    image: 'img/og-social-card.jpg',
    algolia: {
      appId: '7M3BTIV34B',
      // Public API key: it is safe to commit it
      apiKey: '10f3d9d2cd836eec903fcabbd6d50139',
      indexName: 'hasura',
    },
    announcementBar: {
      id: 'announcementBar-5', // Increment on change
      content: `This is the documentation for Hasura DDN, the future of data delivery. <a target="_blank" rel="noopener noreferrer" href="https://hasura.io/docs/latest/index/">Click here for the Hasura v2.x docs</a>.`,
    },
    navbar: {
      title: '',
      hideOnScroll: true,
      logo: {
        alt: 'Hasura Logo',
        src: 'img/logo-dark.svg',
        href: '/index',
        srcDark: '/img/logo-light.svg',
      },
      items: [
        {
          type: 'docsVersionDropdown',
          position: 'left',
          dropdownActiveClassDisabled: true,
          dropdownItemsAfter: [
            {
              href: 'https://promptql.io/docs/',
              label: 'PromptQL',
            },
            {
              href: 'https://hasura.io/docs/2.0/index/',
              label: 'v2.x',
            },
            {
              href: 'https://hasura.io/docs/1.0/graphql/core/index.html',
              label: 'v1.x',
            },
          ],
        },
        // {
        //   type: 'search',
        //   position: 'left',
        // },
        {
          to: 'https://hasura.io/',
          label: 'Hasura.io',
          position: 'right',
        },
        // {
        //   to: 'https://cloud.hasura.io/login',
        //   label: 'Log In',
        //   position: 'right',
        // },
      ],
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['json', 'typescript', 'bash', 'yaml'],
    },
  } satisfies Preset.ThemeConfig,
  markdown: {
    mermaid: true,
  },
  themes: ['@docusaurus/theme-mermaid'],
};

export default config;
