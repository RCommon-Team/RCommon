import PostgreSqlLogo from '@site/static/img/databases/logos/postgresql.png';
import MongoDbLogo from '@site/static/img/databases/logos/mongodb.webp';
import TypeScriptLogo from '@site/static/img/databases/logos/ts.png';
import ClickHouseLogo from '@site/static/img/databases/logos/clickhouse-glyph.png';
import OpenAPILogo from '@site/static/img/databases/logos/openapi.png';
import PythonLogo from '@site/static/img/databases/logos/python.png';
import GraphQlLogo from '@site/static/img/databases/logos/gql.png';
import GoLogo from '@site/static/img/databases/logos/go.png';

export const dataSources = {
  PostgreSQL: {
    name: 'PostgreSQL',
    image: PostgreSqlLogo,
    connectorType: 'datasource',
  },
  MongoDB: {
    name: 'MongoDB',
    image: MongoDbLogo,
    connectorType: 'datasource',
  },
  ClickHouse: {
    name: 'ClickHouse',
    image: ClickHouseLogo,
    connectorType: 'datasource',
  },
  TypeScript: {
    name: 'TypeScript',
    image: TypeScriptLogo,
    connectorType: 'businessLogic',
  },
  Python: {
    name: 'Python',
    image: PythonLogo,
    connectorType: 'businessLogic',
  },
  Go: {
    name: 'Go',
    image: GoLogo,
    connectorType: 'businessLogic',
  },
  OpenAPI: {
    name: 'OpenAPI',
    image: OpenAPILogo,
    connectorType: 'datasource',
  },
  GraphQL: {
    name: 'GraphQL',
    image: GraphQlLogo,
    connectorType: 'datasource',
  },
};
