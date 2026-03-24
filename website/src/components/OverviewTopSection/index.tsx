import Link from '@docusaurus/Link';
import React from 'react';
export const OverviewTopSection = (props: {
  youtubeVideoId: string;
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
      <div className={'video-wrapper'}>
        <div className={'video-aspect-ratio'}>
        <iframe
            src={`https://www.youtube.com/embed/${props.youtubeVideoId}`}
            allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
          />
        </div>
      </div>
    </div>
  )
}