import React, { useEffect } from 'react';

interface CustomGuideDocProps {
  children: React.ReactNode;
}

const CustomGuideDoc: React.FC<CustomGuideDocProps> = ({ children }) => {
  useEffect(() => {
    if (typeof window !== 'undefined') {
      const directives = document.body.querySelectorAll('[class*="heading"]');
      directives.forEach((directive, index) => {
        let next = index + 1;
        directive.setAttribute('data-step', next.toString());
      });

      const steps = document.body.querySelectorAll('[class*="step_container"]');
      if (steps.length > 0) {
        steps[0].setAttribute('style', 'margin-top: 75px');
      }

      const h1 = document.body.getElementsByTagName('h1');
      if (h1.length > 0) {
        h1[0].setAttribute('style', 'margin-top: 75px');
        const p = h1[0].nextElementSibling as HTMLElement;
        if (p) {
          p.setAttribute('style', 'font-size: 1.3rem; font-weight: 300');
        }
      }
    }
  }, []);

  return <div>{children}</div>;
};

export default CustomGuideDoc;
