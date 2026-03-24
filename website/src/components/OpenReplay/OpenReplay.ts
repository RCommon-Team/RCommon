import siteConfig from '@generated/docusaurus.config';
import Tracker from '@openreplay/tracker';
import Cookies from 'js-cookie';

const OPENREPLAY_SESSION_COOKIE = 'openReplaySessionHash';
const OPENREPLAY_INGEST_POINT = siteConfig.customFields.openReplayIngestPoint as string;
const OPENREPLAY_PROJECT_KEY = siteConfig.customFields.openReplayProjectKey as string;

let tracker: Tracker | null = null;

export const initOpenReplay = async () => {
  const { default: Tracker } = await import('@openreplay/tracker');
  tracker = new Tracker({
    projectKey: OPENREPLAY_PROJECT_KEY,
    ingestPoint: OPENREPLAY_INGEST_POINT,
  });
};

export const startOpenReplayTracking = (userId?: string) => {
  if (tracker) {
    const existingSessionHash = Cookies.get(OPENREPLAY_SESSION_COOKIE);

    if (existingSessionHash) {
      // Resume existing session
      tracker.start({
        sessionHash: existingSessionHash,
        userID: userId,
        metadata: {
          domain: window.location.hostname,
        },
      });
    } else {
      // Start a new session
      tracker.start({
        userID: userId,
        metadata: {
          domain: window.location.hostname,
        },
      });
      const newSessionHash = tracker.getSessionToken();
      if (newSessionHash) {
        setCookie(OPENREPLAY_SESSION_COOKIE, newSessionHash);
      }
    }
  } else {
    console.warn('OpenReplay tracker is not initialized');
  }
};

function setCookie(name: string, value: string) {
  const inTenMinutes = new Date(new Date().getTime() + 10 * 60 * 1000);

  return Cookies.set(name, value, {
    expires: inTenMinutes,
    path: '/',
    domain: '.hasura.io',
    sameSite: 'Lax',
    secure: true,
  });
}
