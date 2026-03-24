import React, { useEffect, useState } from 'react';

import Actions from '@site/static/icons/features/actions.svg';
import Auth from '@site/static/icons/shield-tick.svg';
import Basics from '@site/static/icons/book-open-01.svg';
import Builds from '@site/static/icons/builds.svg';
import Collaboration from '@site/static/icons/features/collaborators.svg';
import Community from '@site/static/icons/announcement-02.svg';
import Connectors from '@site/static/icons/event-triggers.svg';
import DataModeling from '@site/static/icons/features/data-modeling.svg';
import Deployment from '@site/static/icons/features/deployment.svg';
import Enterprise from '@site/static/icons/features/enterprise.svg';
import Faq from '@site/static/icons/help-square.svg';
import Federation from '@site/static/icons/data_federation.svg';
import GettingStarted from '@site/static/icons/home-smile.svg';
import Glossary from '@site/static/icons/box.svg';
import GraphQLAPI from '@site/static/icons/graphql-logo.svg';
import JsonAPI from '@site/static/icons/jsonapi-logo-small.svg';
import HasuraCLI from '@site/static/icons/terminal-square.svg';
import Help from '@site/static/icons/features/hasura_policies.svg';
import Introduction from '@site/static/icons/award-02.svg';
import Observability from '@site/static/icons/eye.svg';
import Plugins from '@site/static/icons/remote-schema.svg';
import ProjectConfiguration from '@site/static/icons/dataflow-01.svg';
import PromptQL from '@site/static/icons/features/prompt-ql.svg';
import Quickstart from '@site/static/icons/speedometer-04.svg';
import Recipe from '@site/static/icons/beaker.svg';
import SupergraphModeling from '@site/static/icons/cpu-chip-01.svg';
import Upgrade from '@site/static/icons/cloud-lightning.svg';

import { useColorMode } from '@docusaurus/theme-common';

export function addIconsToLabel(label, className) {
  const { colorMode } = useColorMode();
  const [definedColorMode, setDefinedColorMode] = useState('');

  useEffect(() => {
    setDefinedColorMode(colorMode);
  }, [colorMode]);

  const isDarkMode = definedColorMode === 'dark';

  // Add inline styles for the icon
  const iconStyle = {
    width: '20px',
    height: '20px',
    display: 'block',
  };

  // When creating icons, apply the style
  let icons;
  switch (className) {
    case 'introduction-icon':
      icons = <Introduction style={iconStyle} />;
      break;
    case 'basics-icon':
      icons = <Basics style={iconStyle} />;
      break;
    case 'getting-started-icon':
      icons = <GettingStarted style={iconStyle} />;
      break;
    case 'auth-icon':
      icons = <Auth style={iconStyle} />;
      break;
    case 'connectors-icon':
      icons = <Connectors style={iconStyle} />;
      break;
    case 'plugins-icon':
      icons = <Plugins style={iconStyle} />;
      break;
    case 'data-modeling':
      icons = <DataModeling style={iconStyle} />;
      break;
    case 'graphQL-api-icon':
      icons = <GraphQLAPI style={iconStyle} />;
      break;
    case 'json-api-icon':
      icons = <JsonAPI style={iconStyle} />;
      break;
    case 'ci-cd-icon':
      icons = <CiCd style={iconStyle} />;
      break;
    case 'prompt-ql-icon':
      icons = <PromptQL style={iconStyle} />;
      break;
    case 'project-configuration':
      icons = <ProjectConfiguration style={iconStyle} />;
      break;
    case 'hasura-cli-icon':
      icons = <HasuraCLI style={iconStyle} />;
      break;
    case 'observability-icon':
      icons = <Observability style={iconStyle} />;
      break;
    case 'collaboration-icon':
      icons = <Collaboration style={iconStyle} />;
      break;
    case 'federation-icon':
      icons = <Federation style={iconStyle} />;
      break;
    case 'build-icon':
      icons = <Builds style={iconStyle} />;
      break;
    case 'enterprise-icon':
      icons = <Enterprise style={iconStyle} />;
      break;
    case 'glossary-icon':
      icons = <Glossary style={iconStyle} />;
      break;
    case 'quickstart-icon':
      icons = <Quickstart style={iconStyle} />;
      break;
    case 'supergraph-modeling-icon':
      icons = <SupergraphModeling style={iconStyle} />;
      break;
    case 'faq-icon':
      icons = <Faq style={iconStyle} />;
      break;
    case 'community-icon':
      icons = <Community style={iconStyle} />;
      break;
    case 'logic-icon':
      icons = <Actions style={iconStyle} />;
      break;
    case 'help-icon':
      icons = <Help style={iconStyle} />;
      break;
    case 'deployment':
      icons = <Deployment style={iconStyle} />;
      break;
    case 'private-ddn':
      icons = <Deployment style={iconStyle} />;
      break;
    case 'upgrade':
      icons = <Upgrade style={iconStyle} />;
      break;
    case 'recipes':
      icons = <Recipe style={iconStyle} />;
      break;
  }

  return (
    <div>
      {icons} {label}
    </div>
  );
}
