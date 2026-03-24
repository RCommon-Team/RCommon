export type Connector = {
  name: string;
  title: string;
  logo: string;
  link?: string;
};

export const GRAPHQL_ENDPOINT = `https://conn-deploy-prod.hasura.app/v1/graphql`;

export const QUERY = `query ALL_CONNECTORS_QUERY {
  connector_overview(order_by: {title: asc}) {
    name
    title
    logo
  }
}`;

/* INFO: For some reason, hasura.io/connectors is using this exclusion list instead
of dealing with this in the API. For now, we'll need to use their same array to avoid
rendering connectors that don't have pages.
Details: https://github.com/hasura/hasura.io/blob/df97144f5562e7a674b7929e7b4ed315bfb43610/main-site-nextjs/src/pages/connectors/%5Bslug%5D.tsx#L19-L29 */
const exclusionList = ['typescript-deno', 'sendgrid', 'go', 'spanner'];

function filterExcludedConnectors(connectors: Connector[]) {
  return connectors.filter(connector => !exclusionList.includes(connector.name));
}

export async function fetchConnectors() {
  const response = await fetch(GRAPHQL_ENDPOINT, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      query: QUERY,
    }),
  });

  if (!response.ok) {
    throw new Error(`HTTP error: ${response.status}`);
  }

  const result = await response.json();

  // Deal with the exclusion list
  const connectors: Connector[] = result.data.connector_overview;
  console.log(connectors);
  const filtered = filterExcludedConnectors(connectors);
  console.log(filtered);

  return filtered;
}
