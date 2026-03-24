import React from 'react';
import styles from './styles.module.css';

export const OverviewIconCard = (props: {
  iconName: React.JSX.Element;
}) => {
  return (
    <div className={styles.overviewIconCard}>
      {props.iconName}
    </div>
  )
}