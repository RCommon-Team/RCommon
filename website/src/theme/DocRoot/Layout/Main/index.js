import React, { useEffect } from 'react';
import clsx from 'clsx';
import { useDocsSidebar } from '@docusaurus/theme-common/internal';
import { useLocation } from '@docusaurus/router';
import styles from './styles.module.css';

export default function DocRootLayoutMain({ hiddenSidebarContainer, children }) {
  const sidebar = useDocsSidebar();
  const location = useLocation();

  useEffect(() => {
    // dynamically updating the pg prop for the getting started cta
    function updateGettingStartedParam() {
      const linkElement = document.querySelector('#login_button');

      if (linkElement) {
        let page = location.pathname;
        page = page.slice(0, -1);
        page = page.replace('/docs/', 'docs_ddn_').replace('3.0/', '').replace(/\//g, '_');

        const href = linkElement.getAttribute('href');
        const newHref = href.replace(/pg=([^&]+)/, `pg=${page}`);
        linkElement.setAttribute('href', newHref);
      }
    }

    updateGettingStartedParam();
  }, [location]);

  return (
    <main
      className={clsx(styles.docMainContainer, (hiddenSidebarContainer || !sidebar) && styles.docMainContainerEnhanced)}
    >
      <div
        className={clsx(
          'container padding-top--md padding-bottom--lg',
          styles.docItemWrapper,
          hiddenSidebarContainer && styles.docItemWrapperEnhanced
        )}
      >
        {children}
      </div>
    </main>
  );
}
