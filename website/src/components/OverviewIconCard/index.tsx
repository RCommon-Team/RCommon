import React from 'react';
import styles from './styles.module.css';

export const OverviewIconCard = (props: {
  icon?: React.JSX.Element;
  title?: string;
  description?: string;
  link?: string;
  // Legacy prop name kept for backward compatibility
  iconName?: React.JSX.Element;
}) => {
  return (
    <div className={styles.overviewIconCard}>
      {props.icon ?? props.iconName}
    </div>
  );
};
