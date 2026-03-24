import React, { useEffect, useState } from 'react';
import { useLocation, useHistory } from '@docusaurus/router';
import './styles.css';
import { dataSources } from './dataSources';
import { isBusinessLogicConnector, savePreference } from './utils';
import { getContent } from './contentLoader';
import { Selector } from './selector';

export const DatabaseContentLoader = () => {
  const location = useLocation();
  const history = useHistory();
  const [connectorPreference, setConnectorPreference] = useState<string | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const dbParam = params.get('db');
    const savedPreference = localStorage.getItem('hasuraV3ConnectorPreference');
    const isBusinessLogicExcluded =
      location.pathname.includes('connect-to-data') || location.pathname.includes('mutate-data');

    if (dbParam && dataSources[dbParam]) {
      setConnectorPreference(savePreference(dbParam, history));
    } else if (savedPreference) {
      if (dataSources[savedPreference]) {
        setConnectorPreference(savedPreference);
      } else {
        localStorage.removeItem('hasuraV3ConnectorPreference');
      }

      if (isBusinessLogicExcluded && isBusinessLogicConnector(savedPreference)) {
        setConnectorPreference(null);
      } else {
        setConnectorPreference(savedPreference);
      }

      if (!dbParam && savedPreference && dataSources[savedPreference]) {
        history.replace({
          search: `db=${savedPreference}`,
        });
      }
    }
  }, [location.search, location.pathname]);

  const isBusinessLogicExcluded =
    location.pathname.includes('connect-to-data') || location.pathname.includes('mutate-data');
  const isAddBusinessLogicPage = location.pathname.includes('add-business-logic');

  return (
    <div>
      <Selector
        connectorPreference={connectorPreference}
        setConnectorPreference={setConnectorPreference}
        isBusinessLogicExcluded={isBusinessLogicExcluded}
        isAddBusinessLogicPage={isAddBusinessLogicPage}
        history={history}
      />
      {connectorPreference && (!isBusinessLogicExcluded || !isBusinessLogicConnector(connectorPreference)) ? (
        getContent(connectorPreference, dataSources)
      ) : (
        <div>Please select your source preference.</div>
      )}
    </div>
  );
};
