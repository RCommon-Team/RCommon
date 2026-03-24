import React, { ReactNode } from 'react';
import './styles.css';
import CodeBlock from '@theme/CodeBlock';
import { MDXProvider } from '@mdx-js/react';

interface CodeStepProps {
  id: string;
  language: string;
  code: string | string[];
  heading: string;
  children?: ReactNode;
  output?: string;
}

const CodeStep = (props: CodeStepProps) => {
  return (
    <div className={'step_container'} data-attr={props.id}>
      <div className={'heading'}>
        <h2>{props.heading}</h2>
      </div>
      <div className={'content'}>
        <div className={'description'}>
          <MDXProvider>{props.children}</MDXProvider>
        </div>
        <div className={'code'}>
          {Array.isArray(props.code) ? (
            props.code.map((codeSnippet, index) => (
              <CodeBlock
                key={index}
                className={`language-${props.language} main-block`}
              >
                {codeSnippet}
              </CodeBlock>
            ))
          ) : (
            <CodeBlock
              className={`language-${props.language} main-block`}
            >
              {props.code}
            </CodeBlock>
          )}
          {props.output && (
            <details>
              <summary>Output</summary>
              <CodeBlock className={`language-plaintext`}>
                {props.output}
              </CodeBlock>
            </details>
          )}
        </div>
      </div>
    </div>
  );
};

export default CodeStep;
