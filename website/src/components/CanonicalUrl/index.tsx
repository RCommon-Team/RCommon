import React from 'react';
import { useDoc } from '@docusaurus/theme-common/internal';
import Head from '@docusaurus/Head';

export default function CanonicalUrl() {
  const { metadata } = useDoc();
  const canonicalUrl = metadata.frontMatter?.canonicalUrl as string | undefined;

  if (!canonicalUrl) return null;

  return (
    <Head>
      <link rel="canonical" href={canonicalUrl} />
    </Head>
  );
}
