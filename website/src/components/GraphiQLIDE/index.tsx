import React from 'react';
import GraphiQL from 'graphiql';
import 'graphiql/graphiql.min.css';
import './styles.css';

const GraphiQLIDE = ({ query, variables, response, viewOnly = true }) => {
  const notReal = async ({ query }) => {
    return {
      data: {
        easterEgg: `This query and response is for demo purposes only. Running it doesn't actually hit an API. Refresh the page to see the original response.`,
      },
    };
  };

  // new graphiql is being funky on builds — checking to see if we're in the browser
  const isBrowser = typeof window !== 'undefined';

  if (!isBrowser) {
    return null;
  }

  return (
    <div>
      <GraphiQL
        readOnly={true}
        editorTheme={'dracula'}
        schema={null}
        fetcher={notReal}
        query={query}
        variables={variables}
        response={response}
        isHeadersEditorEnabled={false}
        defaultEditorToolsVisibility={false}
      >
        <GraphiQL.Logo>
          <span
            style={{
              fontFamily: 'sans-serif',
              fontSize: '1.5rem',
              fontWeight: 'bold',
              color: 'white',
            }}
          >
            <GraphiQL.Toolbar />
            {/*<GraphiQL.Footer/>*/}
          </span>
        </GraphiQL.Logo>
      </GraphiQL>
    </div>
  );
};

export default GraphiQLIDE;
