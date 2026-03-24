import React from 'react';
import { useLocation } from '@docusaurus/router';

import PostgreSqlDeploy from '@site/docs/deployment/deploying-your-project/_databaseDocs/_postgreSQL/_03-deploy-a-connector.mdx';
import MongoDBDeploy from '@site/docs/deployment/deploying-your-project/_databaseDocs/_mongoDB/_03-deploy-a-connector.mdx';
import TypeScriptDeploy from '@site/docs/deployment/deploying-your-project/_databaseDocs/_typeScript/_03-deploy-a-connector.mdx';
import ClickHouseDeploy from '@site/docs/deployment/deploying-your-project/_databaseDocs/_clickHouse/_03-deploy-a-connector.mdx';
import OpenAPIDeploy from '@site/docs/deployment/deploying-your-project/_databaseDocs/_openAPI/_03-deploy-a-connector.mdx';
import GraphQlDeploy from '@site/docs/deployment/deploying-your-project/_databaseDocs/_graphql/_03-deploy-a-connector.mdx';
import PythonDeploy from '@site/docs/deployment/deploying-your-project/_databaseDocs/_python/_03-deploy-a-connector.mdx';
import GoDeploy from '@site/docs/deployment/deploying-your-project/_databaseDocs/_go/_03-deploy-a-connector.mdx';

export const getContent = (connectorPreference: string | null, dataSources: any) => {
  const location = useLocation();
  const isBusinessLogicExcluded =
    location.pathname.includes('connect-to-data') || location.pathname.includes('mutate-data');

  if (
    isBusinessLogicExcluded &&
    connectorPreference &&
    dataSources[connectorPreference].connectorType === 'businessLogic'
  ) {
    return <div>Content not available for this selection</div>;
  }

  let pathParts = location.pathname.split('/').filter(Boolean);
  let route = pathParts.pop();

  switch (route) {
    case 'deploy-a-connector':
      switch (connectorPreference) {
        case 'PostgreSQL':
          return <PostgreSqlDeploy />;
        case 'MongoDB':
          return <MongoDBDeploy />;
        case 'ClickHouse':
          return <ClickHouseDeploy />;
        case 'TypeScript':
          return <TypeScriptDeploy />;
        case 'Python':
          return <PythonDeploy />;
        case 'Go':
          return <GoDeploy />;
        case 'OpenAPI':
          return <OpenAPIDeploy />;
        case 'GraphQL':
          return <GraphQlDeploy />;
        default:
          return <div />;
      }
    default:
      return <div>Content not found...</div>;
  }
};
