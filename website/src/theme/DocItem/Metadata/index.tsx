import React from 'react';
import Head from '@docusaurus/Head';
import {PageMetadata} from '@docusaurus/theme-common';
import {useDoc} from '@docusaurus/theme-common/internal';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';

export default function DocItemMetadata(): JSX.Element {
  const {metadata, frontMatter, assets} = useDoc();
  const {siteConfig} = useDocusaurusContext();

  const techArticleJsonLd = {
    '@context': 'https://schema.org',
    '@type': 'TechArticle',
    headline: metadata.title,
    description: metadata.description,
    url: `${siteConfig.url}${metadata.permalink}`,
    publisher: {
      '@type': 'Organization',
      name: 'RCommon Team',
    },
    about: {
      '@type': 'SoftwareApplication',
      name: 'RCommon',
    },
  };

  return (
    <>
      <PageMetadata
        title={metadata.title}
        description={metadata.description}
        keywords={frontMatter.keywords}
        image={assets.image ?? frontMatter.image}
      />
      <Head>
        <script type="application/ld+json">
          {JSON.stringify(techArticleJsonLd)}
        </script>
      </Head>
    </>
  );
}
