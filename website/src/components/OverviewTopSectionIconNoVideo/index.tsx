import Link from '@docusaurus/Link';
import React from 'react';
import { OverviewIconCard } from '@site/src/components/OverviewIconCard';
export const OverviewTopSectionIconNoVideo = (props: {
  icon: React.JSX.Element;
  links: { title: string; href: string }[];
  intro: React.JSX.Element;
}) => {
  return (
    <div className={'front-matter'}>
      <div>
        <div>
          {props.intro}
        </div>
        {props.links.length === 0 ? null : <h4>Quick Links</h4>}
        <ul>
          {props.links.map((link, index) => (
            <li key={index}>
              <Link to={link.href}>{link.title}</Link>
            </li>
          ))}
        </ul>
      </div>
      <div>
        <OverviewIconCard
          iconName={props.icon}
        />
      </div>
    </div>
  )
}