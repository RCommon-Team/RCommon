import { Reviewer } from './types';

// Find the right JSON
function findReviewerByName(reviewers: Reviewer[], name: string): Reviewer | undefined {
  return reviewers.find(reviewer => reviewer.name === name);
}

// Whose turn is it?
export const selectReviewer = (reviewers: Reviewer[]): Reviewer => {
  const rob = findReviewerByName(reviewers, 'Rob');

  if (!rob) {
    throw new Error('Reviewer not found');
  }

  return rob;
};
