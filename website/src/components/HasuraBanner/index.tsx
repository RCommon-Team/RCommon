import React from 'react';
import Link from '@docusaurus/Link';
import ArrowRight from '@site/static/icons/arrow_right.svg';
import Star from '@site/static/img/star.svg';
import hasuraFree from '@site/static/img/hasura-free.png';
import styles from './styles.module.css';

const HasuraBanner = () => {
  return (
    <Link
      className={styles['remove-text-decoration']}
      href="https://hasura.io/docs/3.0/quickstart?pg=docs&plcmt=pre-footer&cta=connect-your-own-data-source&tech=default"
      id="banner-button"
    >
      <div className={styles['hasura-wrapper']}>
        <div className={styles['p40']}>
          <h3>Build an API in Less Than a Minute with Hasura DDN</h3>
          <ul className={styles['desc']}>
            <li>
              <Star />
              Connect your own data source effortlessly
            </li>
            <li>
              <Star />
              Collaborate easily with your teammates and across teams
            </li>
            <li>
              <Star />
              Author and scale your API with declarative metadata
            </li>
          </ul>
          <div className={styles['try-hasura-div']}>
            Try Hasura DDN Today for Free
            <div className={styles['arrow']}>
              <ArrowRight />
            </div>
          </div>
        </div>
        <div className={styles['show-mobile']}>
          <img src={hasuraFree} alt="Promo" />
        </div>
      </div>
    </Link>
  );
};

export default HasuraBanner;
