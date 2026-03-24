import dotenv from 'dotenv';
import { Reviewer } from './types';

dotenv.config();

export const assignGitHubReviewer = async (prUrl: string, assignedReviewer: Reviewer): Promise<Response> => {
  const prNumber = prUrl.split('/').pop();
  const apiUrl = `https://api.github.com/repos/${process.env.REPO_OWNER}/${process.env.REPO_NAME}/pulls/${prNumber}/requested_reviewers`;
  const assignResponse = await fetch(apiUrl, {
    method: 'POST',
    headers: {
      Authorization: `token ${process.env.DOCS_GITHUB_TOKEN}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      reviewers: [assignedReviewer.github_username],
    }),
  });

  if (assignResponse.ok) {
    console.log(`${assignedReviewer.name} has been assigned to review this PR on GitHub.`);
  } else {
    console.error('Error assigning reviewer on GitHub:', assignResponse.statusText);
  }

  return assignResponse;
};
