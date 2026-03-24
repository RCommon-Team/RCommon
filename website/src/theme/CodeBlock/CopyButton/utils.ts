// copyToClipboard utilizes the standard navigator.clipboard API to copy a text value from an HTML page.
export function copyToClipboard(text: string): Promise<void> {
  // First, we'll check to make sure the clipboard API is available. If it is, copy that text.
  if (navigator.clipboard && navigator.clipboard.writeText) {
    return navigator.clipboard.writeText(text);
  } else {
    // Just in case navigator.clipboard isn't supported...which is very unlikely.
    console.log('This is a very old version of a browser.');
  }
}
