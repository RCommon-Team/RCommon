import React, { useState, useEffect } from 'react';
import { useLocation } from '@docusaurus/router';
import { useDocsSidebar } from '@docusaurus/theme-common/internal';
import BackToTopButton from '@theme/BackToTopButton';
import DocRootLayoutSidebar from '@theme/DocRoot/Layout/Sidebar';
import DocRootLayoutMain from '@theme/DocRoot/Layout/Main';
import styles from './styles.module.css';
import useIsBrowser from '@docusaurus/useIsBrowser';
import BrowserOnly from '@docusaurus/BrowserOnly';
import { AiChatBot } from '@site/src/components/AiChatBot/AiChatBot';
import fetchUser from '@theme/DocRoot/Layout/FetchUser';
import posthog from 'posthog-js';
import { initOpenReplay, startOpenReplayTracking } from '@site/src/components/OpenReplay/OpenReplay';

export default function DocRootLayout({ children }) {
  const sidebar = useDocsSidebar();
  const location = useLocation();
  const isBrowser = useIsBrowser();
  const [hiddenSidebarContainer, setHiddenSidebarContainer] = useState(false);
  const [hasInitialized, setHasInitialized] = useState(false);
  const [hasInitializedOpenReplay, setHasInitializedOpenReplay] = useState(false);

  useEffect(() => {
    if (isBrowser && !hasInitialized) {
      (async () => {
        try {
          await initOpenReplay();
          setHasInitializedOpenReplay(true);
        } catch (error) {
          console.error('Failed to initialize OpenReplay:', error);
        }
      })();

      posthog.init('phc_MZpdcQLGf57lyfOUT0XA93R3jaCxGsqftVt4iI4MyUY', {
        api_host: 'https://analytics-posthog.hasura-app.io',
      });

      setHasInitialized(true);
    }
  }, [isBrowser, hasInitialized]);

  useEffect(() => {
    if (isBrowser && hasInitializedOpenReplay && window.location.hostname != 'localhost') {
      startOpenReplayTracking();
    }
  }, [hasInitializedOpenReplay]);

  useEffect(() => {
    if (isBrowser && hasInitialized) {
      posthog.capture('$pageview');
    }

    const getUser = async () => {
      try {
        // const user = await fetchUser();
        // TODO: When the CORS domains in Lux are updated, uncomment this and test on stage.hasura.io
        // posthog.identify(user.data.users[0]?.id, { email: user.data.users[0]?.email });
      } catch (error) {
        console.error('Error fetching user:', error);
      }
    };

    getUser();
  }, [location]);

  return (
    <div className={styles.docsWrapper}>
      <BackToTopButton />
      <div className={styles.docRoot}>
        {sidebar && (
          <DocRootLayoutSidebar
            sidebar={sidebar.items}
            hiddenSidebarContainer={hiddenSidebarContainer}
            setHiddenSidebarContainer={setHiddenSidebarContainer}
          />
        )}
        <DocRootLayoutMain hiddenSidebarContainer={hiddenSidebarContainer}>{children}</DocRootLayoutMain>
      </div>
    </div>
  );
}
