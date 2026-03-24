import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom';
import BrowserOnly from '@docusaurus/BrowserOnly';
import './styles.css';

// Modal Implementation
const Modal = props => {
  const closeOnEscapeKeyDown = e => {
    if (e.key === 'Escape') {
      props.onClose();
    }
  };

  useEffect(() => {
    document.body.addEventListener('keydown', closeOnEscapeKeyDown);
    return () => {
      document.body.removeEventListener('keydown', closeOnEscapeKeyDown);
    };
  }, []);

  if (!props.show) {
    return null;
  }

  return ReactDOM.createPortal(
    <div className={'modal'} onClick={props.onClose}>
      <div className={'modal-content'} onClick={e => e.stopPropagation()}>
        <div className={'modal-body'}>{props.children}</div>
        <div className={'modal-footer'}>
          <button onClick={props.onClose} className={'button'}>
            x
          </button>
        </div>
      </div>
    </div>,
    document.getElementsByTagName('body')[0]
  );
};

const Thumbnail = ({ src, ...restProps }) => {
  const [openModal, setOpenModal] = useState(false);
  const resolvedImage = require(`@site/static${src}`).default;
  return (
    <div className={'thumbnail'}>
      <img
        src={resolvedImage}
        {...restProps}
        className={`${'main-img'} ${restProps.className || ''}`}
        onClick={() => setOpenModal(true)}
      />
      <BrowserOnly>
        {() => (
          <Modal onClose={() => setOpenModal(false)} show={openModal}>
            <img src={resolvedImage} {...restProps} className={`${'modal-img'} ${restProps.className || ''}`} />
          </Modal>
        )}
      </BrowserOnly>
    </div>
  );
};

export default Thumbnail;
