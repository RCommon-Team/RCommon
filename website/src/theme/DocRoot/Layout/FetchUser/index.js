export default async function fetchUser() {
  const url = 'https://data.pro.hasura.io/v1/graphql';

  const headers = {
    Accept: '*/*',
    'Accept-Language': 'en-US,en;q=0.9,es-US;q=0.8,es;q=0.7',
    'Content-Type': 'application/json',
    'Hasura-Client-Name': 'hasura-docs',
    Origin: 'https://hasura.io/docs/3.0',
    Referer: 'https://hasura.io/docs/3.0',
    'Sec-Fetch-Mode': 'cors',
    'Sec-Fetch-Site': 'same-site',
  };

  const body = {
    query: `
    query fetchCurrentUser {
      users {
        id
        email
      }
    }
    `,
  };

  try {
    const response = await fetch(url, {
      method: 'POST',
      headers: headers,
      credentials: 'include',
      mode: 'cors',
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    console.log(data);
    return data;
  } catch (error) {
    console.error('Error fetching user:', error);
    return null;
  }
}
