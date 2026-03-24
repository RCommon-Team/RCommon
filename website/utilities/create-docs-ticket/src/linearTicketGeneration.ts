import dotenv from 'dotenv';
import { Reviewer } from './types';
import { linearClient } from './linearClient';
import { AttachmentPayload } from '@linear/sdk';

dotenv.config();

interface PRInfo {
  prTitle: string;
  prUrl: string;
  assignedReviewer: Reviewer;
}

const getCurrentCycle = async (): Promise<string> => {
  try {
    const team = await linearClient.team(process.env.LINEAR_TEAM_ID!);
    const activeCycle = await team.activeCycle;
    return activeCycle!.id;
  } catch (error) {
    console.error('Error fetching current cycle:', error);
    throw error;
  }
};

export const createLinearTicket = async ({ prTitle, prUrl, assignedReviewer }: PRInfo) => {
  return await linearClient.createIssue({
    teamId: process.env.LINEAR_TEAM_ID!,
    title: `PR Review: DDN Docs - ${prTitle}`,
    description: `Link to PR: ${prUrl}`,
    stateId: process.env.LINEAR_TODO_COLUMN_ID!,
    assigneeId: assignedReviewer.linear_id,
    cycleId: await getCurrentCycle(),
    // 'docs-review' and 'ddn'
    labelIds: ['01eec583-7a16-40a7-99fd-c2347f205ef9', 'a2ae3956-691b-41f1-975d-ca9b35662e5d'],
  });
};

export const addLinkAsAttachmentToTicket = async (prUrl: string, issueId: string): Promise<AttachmentPayload> => {
  try {
    const attachment = await linearClient.attachmentLinkGitHubPR(issueId, prUrl);
    console.log('Link added to ticket:', attachment);
    return attachment;
  } catch (error) {
    console.error('Error adding link to ticket:', error);
    throw error;
  }
};
