import React, { useEffect, useState } from 'react';
import { Connector, fetchConnectors } from './utils';

export default function Gallery() {
  const [connectors, setConnectors] = useState<Connector[]>([]);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const data = await fetchConnectors();
        setConnectors(data);
      } catch (err) {
        console.log(`Error fetching connectors: ${err}`);
      }
    };

    fetchData();
  }, []);

  return (
    <ul>
      {connectors.map(connector => (
        <li>{connector.title}</li>
      ))}
    </ul>
  );
}
