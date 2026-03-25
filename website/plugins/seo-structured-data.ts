import type { Plugin } from '@docusaurus/types';

const SOFTWARE_APPLICATION_JSONLD = {
  '@context': 'https://schema.org',
  '@type': 'SoftwareApplication',
  name: 'RCommon',
  description:
    'Open-source .NET infrastructure library providing battle-tested abstractions for persistence, CQRS, event handling, messaging, caching, and more.',
  url: 'https://rcommon.com',
  applicationCategory: 'DeveloperApplication',
  operatingSystem: '.NET 8, .NET 9, .NET 10',
  license: 'https://opensource.org/licenses/Apache-2.0',
  offers: {
    '@type': 'Offer',
    price: '0',
    priceCurrency: 'USD',
  },
  sourceOrganization: {
    '@type': 'Organization',
    name: 'RCommon Team',
    url: 'https://github.com/RCommon-Team',
  },
};

export default function seoStructuredDataPlugin(): Plugin {
  return {
    name: 'seo-structured-data',
    injectHtmlTags() {
      return {
        headTags: [
          {
            tagName: 'script',
            attributes: {
              type: 'application/ld+json',
            },
            innerHTML: JSON.stringify(SOFTWARE_APPLICATION_JSONLD),
          },
        ],
      };
    },
  };
}
