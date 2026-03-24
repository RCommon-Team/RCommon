import React from 'react';
import Link from '@docusaurus/Link';
import useBaseUrl from '@docusaurus/useBaseUrl';
import styles from './styles.module.css';

const CustomFooter = () => {
  return (
    <footer className={styles['custom-footer-wrapper']}>
      <div className={styles['logo-wrapper']}>
        <img src={useBaseUrl('/img/logo-dark.svg')} className={styles['dark-theme-logo']} alt="RCommon" />
        <img src={useBaseUrl('/img/logo.svg')} className={styles['light-theme-logo']} alt="RCommon" />
      </div>
      <div className={styles['copyright']}>{`© ${new Date().getFullYear()} RCommon Team. All rights reserved`}</div>
      <div className={styles['footerSocialIconsWrapper']}>
        <div className={styles['socialBrands']}>
          <Link
            href={'https://github.com/RCommon-Team/RCommon'}
            rel="noopener noreferrer"
            aria-label={'GitHub'}
          >
            GitHub
          </Link>
        </div>
        <div className={styles['socialBrands']}>
          <Link
            href={'https://www.nuget.org/profiles/RCommon'}
            rel="noopener noreferrer"
            aria-label={'NuGet'}
          >
            NuGet
          </Link>
        </div>
      </div>
    </footer>
  );
};

export default CustomFooter;
