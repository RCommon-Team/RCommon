import React, { useEffect, useState } from 'react';
import {useThemeConfig} from '@docusaurus/theme-common';
import {useAnnouncementBar} from '@docusaurus/theme-common/internal';
// import AnnouncementBarCloseButton from '@theme/AnnouncementBar/CloseButton';
import AnnouncementBarContent from '@theme/AnnouncementBar/Content';
import { useColorMode } from '@docusaurus/theme-common';
import AnnouncementBarCloseBtn from '@site/static/icons/x-close.svg';
import AnnouncementBarCloseBtnDark from '@site/static/icons/x-close-dark.svg'

import styles from './styles.module.css';
export default function AnnouncementBar() {
  const {announcementBar} = useThemeConfig();
  const {isActive, close} = useAnnouncementBar();
  if (!isActive) {
    return null;
  }
  const {backgroundColor, textColor, isCloseable} = announcementBar;
  const { colorMode } = useColorMode();
  const [definedColorMode, setDefinedColorMode] = useState('');

  useEffect(() => {
    setDefinedColorMode(colorMode);
  }, [colorMode]);

  const isDarkMode = definedColorMode === 'dark';

  return (
    <div className={styles.announcementWrapper}>
      <div
        className={styles.announcementBar}
        style={{backgroundColor, color: textColor}}
        role="banner">
        {/* {isCloseable && <div className={styles.announcementBarPlaceholder} />} */}
        {isCloseable && (
          <div
            onClick={close}
            className={styles.announcementBarClose}
          >
            { isDarkMode ? <AnnouncementBarCloseBtnDark /> : <AnnouncementBarCloseBtn /> }
          </div>
        )}
        <AnnouncementBarContent className={styles.announcementBarContent} />
        {/* {isCloseable && (
          <AnnouncementBarCloseButton
            onClick={close}
            className={styles.announcementBarClose}
          />
        )} */}
      </div>
    </div>
  );
}
