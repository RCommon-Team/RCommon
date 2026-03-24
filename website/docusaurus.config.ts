import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'RCommon',
  tagline: 'Build Enterprise Applications Without Reinventing the Wheel',
  favicon: 'img/favicon.ico',
  url: 'https://rcommon.com',
  baseUrl: '/',
  organizationName: 'RCommon-Team',
  projectName: 'RCommon',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  trailingSlash: false,

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/RCommon-Team/RCommon/tree/main/website/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themes: [
    '@docusaurus/theme-mermaid',
    [
      require.resolve('@easyops-cn/docusaurus-search-local'),
      {
        hashed: true,
        language: ['en'],
        highlightSearchTermsOnTargetPage: true,
        explicitSearchResultPath: true,
      },
    ],
  ],

  markdown: {
    mermaid: true,
  },

  themeConfig: {
    image: 'img/og-social-card.jpg',
    navbar: {
      title: '',
      logo: {
        alt: 'RCommon Logo',
        src: 'img/rcommon-logo-light-bg.png',
        srcDark: 'img/rcommon-logo-dark-bg.png',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          type: 'docsVersionDropdown',
          position: 'right',
        },
        {
          href: 'https://github.com/RCommon-Team/RCommon',
          label: 'GitHub',
          position: 'right',
        },
        {
          href: 'https://www.nuget.org/profiles/RCommon',
          label: 'NuGet',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            { label: 'Getting Started', to: '/docs/getting-started/overview' },
            { label: 'Persistence', to: '/docs/category/persistence' },
            { label: 'CQRS & Mediator', to: '/docs/category/cqrs--mediator' },
          ],
        },
        {
          title: 'Community',
          items: [
            { label: 'GitHub', href: 'https://github.com/RCommon-Team/RCommon' },
            { label: 'NuGet', href: 'https://www.nuget.org/profiles/RCommon' },
          ],
        },
        {
          title: 'More',
          items: [
            { label: 'License', href: 'https://github.com/RCommon-Team/RCommon/blob/main/LICENSE' },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} RCommon Team. Apache 2.0 License.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'json', 'bash', 'markup'],
    },
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
  } satisfies Preset.ThemeConfig,

  plugins: [
    async function tailwindPlugin(context, options) {
      return {
        name: 'tailwind-plugin',
        configurePostCss(postcssOptions) {
          postcssOptions.plugins.push(require('tailwindcss'));
          postcssOptions.plugins.push(require('autoprefixer'));
          return postcssOptions;
        },
      };
    },
  ],
};

export default config;
