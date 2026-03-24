const utilities = require('./utilities');

const main = async () => {
  try {
    await utilities.writeVersionToFile();
  } catch (error) {
    console.error('Failed to fetch and write version:', error);
  }
};

main();
