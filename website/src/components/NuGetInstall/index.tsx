import React, {useState} from 'react';
import styles from './styles.module.css';

interface NuGetInstallProps {
  packageName: string;
  version?: string;
}

export default function NuGetInstall({packageName, version}: NuGetInstallProps): JSX.Element {
  const [copied, setCopied] = useState(false);
  const command = version
    ? `dotnet add package ${packageName} --version ${version}`
    : `dotnet add package ${packageName}`;

  const handleCopy = () => {
    navigator.clipboard.writeText(command);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className={styles.container}>
      <div className={styles.label}>NuGet Package</div>
      <div className={styles.commandRow}>
        <code className={styles.command}>{command}</code>
        <button className={styles.copyButton} onClick={handleCopy} title="Copy to clipboard">
          {copied ? '✓' : '📋'}
        </button>
      </div>
    </div>
  );
}
