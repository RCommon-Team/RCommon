import { useState, useEffect, useRef } from 'react';
import styles from './styles.module.css';
import useBaseUrl from '@docusaurus/useBaseUrl';

export default function CopyLLM() {
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
  const [isOpen, setIsOpen] = useState(false);
  const [fileContent, setFileContent] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  const FILE_NAME = 'llms-full.txt';

  const fileUrl = useBaseUrl(`/${FILE_NAME}`);

  // Prefetch the file once so that clipboard write can happen synchronously
  useEffect(() => {
    let isMounted = true;
    fetch(fileUrl)
      .then(res => {
        if (!res.ok) throw new Error('Failed to fetch file.');
        return res.text();
      })
      .then(text => {
        if (isMounted) setFileContent(text);
      })
      .catch(err => {
        // Silently fail – try again later in the click handler
        console.warn('Prefetch failed', err);
      });

    return () => {
      isMounted = false;
    };
  }, [fileUrl]);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  /**
   * Cross-browser clipboard write with Safari fallback.
   */
  async function copyToClipboard(text: string) {
    // Try modern API first
    if (navigator.clipboard && navigator.clipboard.writeText) {
      try {
        await navigator.clipboard.writeText(text);
        return;
      } catch (err) {
        console.warn('navigator.clipboard.writeText failed, falling back', err);
      }
    }

    // Fallback for Safari / older browsers
    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.setAttribute('readonly', '');
    textarea.style.position = 'absolute';
    textarea.style.left = '-9999px';
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand('copy');
    document.body.removeChild(textarea);
  }

  async function handleCopy() {
    let showLoadingTimer: NodeJS.Timeout | undefined;

    try {
      // Show loading state if the operation takes noticeable time
      showLoadingTimer = setTimeout(() => {
        setStatus('loading');
      }, 300);

      // Use prefetched content when available to keep copy within user-gesture
      let text = fileContent;
      if (!text) {
        const response = await fetch(fileUrl);
        if (!response.ok) {
          throw new Error('Failed to fetch file.');
        }
        text = await response.text();
        setFileContent(text); // cache for the next time
      }

      await copyToClipboard(text);

      clearTimeout(showLoadingTimer);
      setStatus('success');

      setTimeout(() => {
        setStatus('idle');
      }, 2000);
    } catch (err) {
      console.error(err);
      clearTimeout(showLoadingTimer);
      setStatus('error');
      setTimeout(() => {
        setStatus('idle');
      }, 2000);
    }
  }

  async function handleDownload() {
    try {
      const response = await fetch(fileUrl);
      if (!response.ok) {
        throw new Error('Failed to fetch file.');
      }
      const text = await response.text();
      const blob = new Blob([text], { type: 'text/markdown' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = FILE_NAME;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error(err);
    }
  }

  return (
    <div className={styles.container} ref={containerRef}>
      <button className={styles.ellipsisButton} onClick={() => setIsOpen(!isOpen)} aria-label="Document actions">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="5" cy="12" r="1" />
          <circle cx="12" cy="12" r="1" />
          <circle cx="19" cy="12" r="1" />
        </svg>
      </button>

      {isOpen && (
        <div className={styles.dropdown}>
          <button className={styles.dropdownItem} onClick={handleCopy} disabled={status === 'loading'}>
            {status === 'loading'
              ? 'Copying...'
              : status === 'success'
              ? '✅ Copied!'
              : status === 'error'
              ? 'Failed to copy'
              : 'Copy all docs content to clipboard for use in LLM prompts'}
          </button>
          <button className={styles.dropdownItem} onClick={handleDownload}>
            Download docs content
          </button>
        </div>
      )}
    </div>
  );
}
