import React from 'react';
// import latestVersion from '../../../utilities/update-cli-version/latest-version.json';

const Index: React.FC = () => {
  //  TODO: fetch from the latest.json on CLI download bucket. https://graphql-engine-cdn.hasura.io/ddn/cli/<revision>/latest.json
  // const version = latestVersion.tag_name;

  return (
    <div>
      <p>
        {/*You can download the CLI binary below. The latest version of the CLI is <kbd>{version}</kbd>. Please follow the*/}
        {/*instructions for your system.*/}
        Download the CLI binary below. Please follow the instructions for your system.
      </p>
    </div>
  );
};

export default Index;
