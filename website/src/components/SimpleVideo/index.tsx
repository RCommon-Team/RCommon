import React from 'react';
import styles from './styles.module.css';

const index = ({ videoLink }) => {
  return (
    <div className={styles['iframe-container']}>
      <iframe
        src={videoLink}
        allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture"
        allowFullScreen
        title="Explore a Finished Supergraph video"
        className="max-w-full"
      />
    </div>
  );
};

export default index;
