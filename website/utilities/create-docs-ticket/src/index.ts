import { Reviewer } from './types';
import { selectReviewer } from './selectReviewer';
import { createLinearTicket, addLinkAsAttachmentToTicket } from './linearTicketGeneration';
import { assignGitHubReviewer } from './assignGitHubReviewer';
import { addGitHubCommentToPr } from './githubComment';
import dotenv from 'dotenv';

dotenv.config();

const requiredEnvVars = ['LINEAR_TEAM_ID', 'ROB_INFO', 'REPO_OWNER', 'REPO_NAME', 'DOCS_GITHUB_TOKEN'];
for (const envVar of requiredEnvVars) {
  if (!process.env[envVar]) {
    throw new Error(`Missing required environment variable: ${envVar}`);
  }
}

const title = process.argv[2];
const url = process.argv[3];

if (!title || !url) {
  console.error('Usage: npx ts-node index.ts <title> <url>');
  process.exit(1);
}

const main = async (title: string, url: string): Promise<string> => {
  try {
    const reviewerList: Reviewer[] = [JSON.parse(process.env.ROB_INFO!)];
    const reviewer = selectReviewer(reviewerList);
    const ticket = await createLinearTicket({ prTitle: title, prUrl: url, assignedReviewer: reviewer });
    const issue = await ticket.issue;
    if (issue) {
      await addLinkAsAttachmentToTicket(url, issue.id);
    }
    await assignGitHubReviewer(url, reviewer);
    const commentResponse = await addGitHubCommentToPr(url, reviewer);
    return commentResponse;
  } catch (error) {
    console.error('An error occurred:', error);
    throw error;
  }
};

main(title, url);
