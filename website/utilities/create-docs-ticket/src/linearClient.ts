import { LinearClient } from '@linear/sdk';
require('dotenv').config();

export const linearClient = new LinearClient({
  apiKey: process.env.LINEAR_API_KEY,
});
