import React, { ReactNode, useState, useEffect } from 'react';
import styles from './styles.module.css';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';

export const Feedback = ({ metadata }: { metadata: any }) => {
  const [rating, setRating] = useState<1 | 2 | 3 | 4 | 5 | null>(null);
  const [notes, setNotes] = useState<string | null>(null);
  const [errorText, setErrorText] = useState<string | null>(null);
  const [hoveredScore, setHoveredScore] = useState<number | null>(null);
  const [textAreaLabel, setTextAreaLabel] = useState<ReactNode | null>(null);
  const [textAreaPlaceholder, setTextAreaPlaceholder] = useState<string>('This section is optional ✌️');
  const [isSubmitSuccess, setIsSubmitSuccess] = useState<boolean>(false);
  const [isActive, setIsActive] = useState<boolean>(false);

  const {
    siteConfig: { customFields },
  } = useDocusaurusContext();

  const { docsServerURL, hasuraVersion, DEV_TOKEN } = customFields as {
    docsServerURL: string;
    hasuraVersion: number;
    DEV_TOKEN?: string;
  };

  useEffect(() => {
    const popups = document.querySelectorAll('.chat-popup');
    popups.forEach(popup => {
      popup.style.display = isActive ? 'none' : '';
    });
  }, [isActive]);

  const submitDisabled = rating === null || (rating < 4 && (notes === null || notes === ''));

  const scores: (1 | 2 | 3 | 4 | 5)[] = [1, 2, 3, 4, 5];

  const handleSubmit = async () => {
    if (rating === null) {
      setErrorText('Please select a score.');
      return;
    }

    if (rating < 4 && notes === null) {
      setErrorText(
        "Because this doc wasn't up to scratch please provide us with some feedback of where we can improve."
      );
      return;
    }

    const sendData = async () => {
      const myHeaders = new Headers();
      myHeaders.append('Content-Type', 'application/json');
      myHeaders.append('devtoken', DEV_TOKEN);

      // { pageTitle, pageUrl, score, userFeedback, version, docsUserId }

      const storedUserID = localStorage.getItem('hasuraDocsUserID') as string | 'null';
      const destinationUrl = docsServerURL + '/feedback/public-new-feedback';

      const raw = JSON.stringify({
        score: rating,
        userFeedback: notes,
        pageTitle: document.title,
        pageUrl: window.location.href,
        version: hasuraVersion,
        docsUserId: storedUserID,
      });

      const requestOptions = {
        method: 'POST',
        headers: myHeaders,
        body: raw,
        redirect: 'follow' as RequestRedirect,
      };

      fetch(destinationUrl, requestOptions)
        .then(response => {
          console.log('Feedback submission status:', response.ok ? 'Success' : 'Failed');
          return response.text();
        })
        .catch(error => {
          console.error('Feedback submission failed:', error);
        });
    };

    sendData()
      .then(() => {
        // saTrack('Responded to Did You Find This Page Helpful', {
        //   label: 'Responded to Did You Find This Page Helpful',
        //   response: rating >= 4 ? 'YES' : 'NO',
        //   pageUrl: window.location.href,
        // });
        setRating(null);
        setNotes(null);
        setIsSubmitSuccess(true);
      })
      .catch(e => {
        console.error(e);
      });

    return;
  };

  const handleScoreClick = (scoreItem: 1 | 2 | 3 | 4 | 5) => {
    if (scoreItem === rating) {
      setRating(null);
      setErrorText(null);
      setHoveredScore(null);
      setIsActive(false);
      return;
    }
    setIsActive(true);
    setErrorText(null);
    setRating(scoreItem);
    // // feedback scroll
    if (typeof window !== 'undefined') {
      window.location.hash = 'feedback';
    }
    if (scoreItem < 4) {
      setTextAreaLabel(
        <>
          <p>What can we do to improve it? Please be as detailed as you like.</p>
          <p>Real human beings read every single review.</p>
        </>
      );
      setTextAreaPlaceholder('This section is required... how can we do better? ✍️');
    }
    if (scoreItem >= 4) {
      setTextAreaLabel(
        <>
          <p>Any general feedback you'd like to add?</p>
          <p>We'll take it all... tell us how well we're doing or where we can improve.</p>
          <p>Real human beings read every single review.</p>
        </>
      );
      setTextAreaPlaceholder('This section is optional ✌️');
    }
  };

  // Do not show on Intro page
  if (metadata.source === '@site/docs/index.mdx') {
    return null;
  }

  return (
    <div className={styles.feedback} id={'feedback'}>
      <div className={styles.form}>
        <div className={styles.topSection}>
          <h4>Was this helpful?</h4>
          {isSubmitSuccess ? (
            <div className={styles.successMessage}>
              <p>Thanks for your feedback.</p>
              {rating >= 3 ? (
                <p>Feel free to review as many docs pages as you like!</p>
              ) : (
                <p>
                  If you need help with the issue that led to this low score, you can create a{' '}
                  <a
                    href="https://github.com/hasura/graphql-engine/issues/new/choose"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    GitHub issue
                  </a>{' '}
                  if you think this is a bug, or check out our{' '}
                  <a href="https://hasura.io/discord" target="_blank" rel="noopener noreferrer">
                    Discord server
                  </a>
                  , where Hasurians and community users are ready to engage.
                </p>
              )}
            </div>
          ) : (
            <div className={styles.numberRow}>
              {scores.map((star, index) => (
                <div
                  className={styles.star}
                  key={star}
                  onClick={() => handleScoreClick(star)}
                  onMouseEnter={() => setHoveredScore(index + 1)}
                  onMouseLeave={() => setHoveredScore(-1)}
                >
                  {rating >= star ? (
                    // <svg width="36" height="36" viewBox="0 0 24 24">
                    //   <path
                    //     fill="#ffc107"
                    //     d="M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z"
                    //   />
                    // </svg>
                    <svg width="32" height="32" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path
                        d="M15.0439 4.60446C15.3512 3.98188 15.5048 3.67058 15.7134 3.57113C15.8949 3.48459 16.1058 3.48459 16.2873 3.57113C16.4959 3.67058 16.6495 3.98188 16.9568 4.60446L19.8724 10.5111C19.9631 10.6949 20.0085 10.7868 20.0748 10.8581C20.1335 10.9213 20.2039 10.9725 20.2821 11.0089C20.3704 11.0499 20.4718 11.0648 20.6746 11.0944L27.1963 12.0476C27.8831 12.148 28.2265 12.1982 28.3854 12.366C28.5236 12.5119 28.5887 12.7124 28.5623 12.9117C28.5321 13.1408 28.2835 13.3829 27.7863 13.8672L23.0689 18.4619C22.9219 18.6052 22.8483 18.6768 22.8009 18.762C22.7589 18.8374 22.7319 18.9203 22.7215 19.006C22.7098 19.1029 22.7272 19.204 22.7619 19.4064L23.8749 25.8962C23.9923 26.5807 24.051 26.9229 23.9407 27.126C23.8447 27.3027 23.6741 27.4267 23.4764 27.4633C23.2492 27.5055 22.9418 27.3438 22.3271 27.0206L16.4968 23.9545C16.3152 23.859 16.2243 23.8112 16.1286 23.7924C16.0439 23.7758 15.9568 23.7758 15.872 23.7924C15.7764 23.8112 15.6855 23.859 15.5039 23.9545L9.67356 27.0206C9.05888 27.3438 8.75154 27.5055 8.52429 27.4633C8.32657 27.4267 8.15596 27.3027 8.05998 27.126C7.94966 26.9229 8.00836 26.5807 8.12576 25.8962L9.23884 19.4064C9.27354 19.204 9.29089 19.1029 9.27915 19.006C9.26876 18.9203 9.24181 18.8374 9.1998 18.762C9.15236 18.6768 9.07883 18.6052 8.93177 18.4619L4.2144 13.8672C3.71721 13.3829 3.46861 13.1408 3.43836 12.9117C3.41204 12.7124 3.47706 12.5119 3.61533 12.366C3.77424 12.1982 4.11762 12.148 4.80438 12.0476L11.3261 11.0944C11.5289 11.0648 11.6303 11.0499 11.7186 11.0089C11.7968 10.9725 11.8672 10.9213 11.9259 10.8581C11.9922 10.7868 12.0376 10.6949 12.1283 10.5111L15.0439 4.60446Z"
                        stroke="#3970FD"
                        stroke-width="1.5"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      />
                    </svg>
                  ) : (
                    // <svg width="36" height="36" viewBox="0 0 24 24">
                    //   <path
                    //     fill={hoveredScore > index ? '#ffc107' : '#B1BCC7'}
                    //     d="M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z"
                    //   />
                    // </svg>
                    <svg width="32" height="32" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path
                        d="M15.0439 4.60446C15.3512 3.98188 15.5048 3.67058 15.7134 3.57113C15.8949 3.48459 16.1058 3.48459 16.2873 3.57113C16.4959 3.67058 16.6495 3.98188 16.9568 4.60446L19.8724 10.5111C19.9631 10.6949 20.0085 10.7868 20.0748 10.8581C20.1335 10.9213 20.2039 10.9725 20.2821 11.0089C20.3704 11.0499 20.4718 11.0648 20.6746 11.0944L27.1963 12.0476C27.8831 12.148 28.2265 12.1982 28.3854 12.366C28.5236 12.5119 28.5887 12.7124 28.5623 12.9117C28.5321 13.1408 28.2835 13.3829 27.7863 13.8672L23.0689 18.4619C22.9219 18.6052 22.8483 18.6768 22.8009 18.762C22.7589 18.8374 22.7319 18.9203 22.7215 19.006C22.7098 19.1029 22.7272 19.204 22.7619 19.4064L23.8749 25.8962C23.9923 26.5807 24.051 26.9229 23.9407 27.126C23.8447 27.3027 23.6741 27.4267 23.4764 27.4633C23.2492 27.5055 22.9418 27.3438 22.3271 27.0206L16.4968 23.9545C16.3152 23.859 16.2243 23.8112 16.1286 23.7924C16.0439 23.7758 15.9568 23.7758 15.872 23.7924C15.7764 23.8112 15.6855 23.859 15.5039 23.9545L9.67356 27.0206C9.05888 27.3438 8.75154 27.5055 8.52429 27.4633C8.32657 27.4267 8.15596 27.3027 8.05998 27.126C7.94966 26.9229 8.00836 26.5807 8.12576 25.8962L9.23884 19.4064C9.27354 19.204 9.29089 19.1029 9.27915 19.006C9.26876 18.9203 9.24181 18.8374 9.1998 18.762C9.15236 18.6768 9.07883 18.6052 8.93177 18.4619L4.2144 13.8672C3.71721 13.3829 3.46861 13.1408 3.43836 12.9117C3.41204 12.7124 3.47706 12.5119 3.61533 12.366C3.77424 12.1982 4.11762 12.148 4.80438 12.0476L11.3261 11.0944C11.5289 11.0648 11.6303 11.0499 11.7186 11.0089C11.7968 10.9725 11.8672 10.9213 11.9259 10.8581C11.9922 10.7868 12.0376 10.6949 12.1283 10.5111L15.0439 4.60446Z"
                        stroke={hoveredScore > index ? '#3970FD' : '#4D5761'}
                        stroke-width="1.5"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      />
                    </svg>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
        <div style={rating ? { display: 'block' } : { display: 'none' }}>
          <div className={styles.textAreaLabel}>{textAreaLabel}</div>
          <textarea
            className={styles.textarea}
            value={notes ?? ''}
            placeholder={textAreaPlaceholder ?? ''}
            rows={5}
            onChange={e => setNotes(e.target.value)}
          />
          <div className={styles.errorAndButton}>
            <p className={styles.errorText}>{errorText}</p>
            <div className={styles.buttonContainer}>
              <button
                id="feedback-btn"
                className={submitDisabled ? styles.buttonDisabled : ''}
                onClick={() => handleSubmit()}
              >
                Send your review!
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
