import dotenv from 'dotenv';
import { Reviewer } from './types';

dotenv.config();

const generateGhComment = (author: string, reviewer: Reviewer): string => {
  return `@${author} Thanks for your PR! I've assigned @${reviewer.github_username} to review it.`;
};

const getPrAuthor = async (prUrl: string): Promise<string | null> => {
  const prNumber = prUrl.split('/').pop();
  const apiUrl = `https://api.github.com/repos/${process.env.REPO_OWNER}/${process.env.REPO_NAME}/pulls/${prNumber}`;
  const response = await fetch(apiUrl, {
    method: 'GET',
    headers: {
      Authorization: `token ${process.env.DOCS_GITHUB_TOKEN}`,
      'Content-Type': 'application/json',
    },
  });
  const pr = await response.json();
  return pr.user?.login || null;
};

export const addGitHubCommentToPr = async (prUrl: string, assignedReviewer: Reviewer): Promise<string> => {
  const prNumber = prUrl.split('/').pop();
  const apiUrl = `https://api.github.com/repos/${process.env.REPO_OWNER}/${process.env.REPO_NAME}/issues/${prNumber}/comments`;
  const author = await getPrAuthor(prUrl);
  if (!author) {
    console.error('Error getting author, skipping comment');
    return '';
  }
  const addComment = await fetch(apiUrl, {
    method: 'POST',
    headers: {
      Authorization: `token ${process.env.DOCS_GITHUB_TOKEN}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      body: `${generateGhComment(author, assignedReviewer)}`,
    }),
  });
  const comment = await addComment.json();
  if (comment.id) {
    console.log('Comment added to PR');
  } else {
    console.log(`Error adding comment to PR: ${comment.message}`);
  }
  return comment;
};
