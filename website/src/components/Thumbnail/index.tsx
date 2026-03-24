import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom';
import BrowserOnly from '@docusaurus/BrowserOnly';
import { CSSTransition } from 'react-transition-group';
import './styles.css';

// Modal Implementation Ref: https://medium.com/tinyso/how-to-create-a-modal-component-in-react-from-basic-to-advanced-a3357a2a716a
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

  return ReactDOM.createPortal(
    <CSSTransition in={props.show} unmountOnExit timeout={{ enter: 0, exit: 300 }}>
      <div className={'modal'} onClick={props.onClose}>
        <div className={'modal-content'} onClick={e => e.stopPropagation()}>
          <div className={'modal-body'}>{props.children}</div>
          <div className={'modal-footer'}>
            <button onClick={props.onClose} className={'button'}>
              x
            </button>
          </div>
        </div>
      </div>
    </CSSTransition>,
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
