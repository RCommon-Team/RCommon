import React from 'react';
import {useThemeConfig} from '@docusaurus/theme-common';
import {useAnnouncementBar} from '@docusaurus/theme-common/internal';
import AnnouncementBarContent from '@theme/AnnouncementBar/Content';

import styles from './styles.module.css';

export default function AnnouncementBar() {
  const {announcementBar} = useThemeConfig();
  const {isActive, close} = useAnnouncementBar();
  if (!isActive) {
    return null;
  }
  const {backgroundColor, textColor, isCloseable} = announcementBar;

  return (
    <div className={styles.announcementWrapper}>
      <div
        className={styles.announcementBar}
        style={{backgroundColor, color: textColor}}
        role="banner">
        {isCloseable && (
          <div
            onClick={close}
            className={styles.announcementBarClose}
            aria-label="Close"
          >
            &times;
          </div>
        )}
        <AnnouncementBarContent className={styles.announcementBarContent} />
      </div>
    </div>
  );
}
