export const isBusinessLogicConnector = (savedPreference: string) =>
  ['TypeScript', 'Python', 'Go'].includes(savedPreference);

export const savePreference = (preference: string, history: any) => {
  localStorage.setItem('hasuraV3ConnectorPreference', preference);

  history.push({
    search: `db=${preference}`,
  });

  return preference;
};
