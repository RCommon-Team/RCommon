import ChevronRight from '@site/static/icons/chevron-right.svg';
import Link from '@docusaurus/Link';
import React from 'react';
export const OverviewPlainCard = (props: { title: string; body: string | React.JSX.Element; link: string; linkText: string }) => {
  return (
    <div className={'card'}>
      <div className={'card-content-items'}>
        <div className={'card-content'}>
          <h4>
            {props.title}
          </h4>
          <p>{props.body}</p>
        </div>
      </div>
      <Link href={props.link}>{props.linkText}<ChevronRight /></Link>
    </div>
  )
}
