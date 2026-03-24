import React from 'react';
import { useThemeConfig, ErrorCauseBoundary } from '@docusaurus/theme-common';
import { splitNavbarItems, useNavbarMobileSidebar } from '@docusaurus/theme-common/internal';
import NavbarItem from '@theme/NavbarItem';
import NavbarColorModeToggle from '@theme/Navbar/ColorModeToggle';
import SearchBar from '@theme/SearchBar';
import NavbarMobileSidebarToggle from '@theme/Navbar/MobileSidebar/Toggle';
import NavbarLogo from '@theme/Navbar/Logo';
import NavbarSearch from '@theme/Navbar/Search';
import styles from './styles.module.css';
// import { useColorMode } from '@docusaurus/theme-common';
// import DocsLogoDark from '@site/static/img/docs-logo-dark.svg';
import DocsLogoLight from '@site/static/img/docs-logo-light.svg';
import BrowserOnly from '@docusaurus/BrowserOnly';
import { AiChatBot } from '@site/src/components/AiChatBot/AiChatBot';
import CopyLLM from '@site/src/components/CopyLlmText';

function useNavbarItems() {
  // TODO temporary casting until ThemeConfig type is improved
  return useThemeConfig().navbar.items;
}
function NavbarItems({ items }) {
  return (
    <>
      {items.map((item, i) => (
        <ErrorCauseBoundary
          key={i}
          onError={error =>
            new Error(
              `A theme navbar item failed to render.
Please double-check the following navbar item (themeConfig.navbar.items) of your Docusaurus config:
${JSON.stringify(item, null, 2)}`,
              { cause: error }
            )
          }
        >
          <NavbarItem {...item} />
        </ErrorCauseBoundary>
      ))}
    </>
  );
}
function NavbarContentLayout({ left, right, searchBarItem }) {
  return (
    <div className="navbar__inner">
      <div className="navbar__items">{left}</div>
      {!searchBarItem && (
        <NavbarSearch>
          <SearchBar />
        </NavbarSearch>
      )}
      <div className="navbar__items navbar__items--right">
        {right}
        <a
          href="https://console.hasura.io/?pg=products&plcmt=header&cta=get_started&tech=default&utm_source=docsv3"
          id="login_button"
          className={'navbar__item navbar__link ' + styles.navBarBtn}
        >
          Log In
          <svg width="20" height="21" viewBox="0 0 20 21" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M7.5 15.5L12.5 10.5L7.5 5.5"
              stroke="white"
              stroke-width="1.5"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </a>
      </div>
    </div>
  );
}
export default function NavbarContent() {
  const mobileSidebar = useNavbarMobileSidebar();
  const items = useNavbarItems();
  const [leftItems, rightItems] = splitNavbarItems(items);
  const searchBarItem = items.find(item => item.type === 'search');
  // const { colorMode } = useColorMode();
  // const [definedColorMode, setDefinedColorMode] = useState('');
  // useEffect(() => {
  //   setDefinedColorMode(colorMode);
  // }, [colorMode]);

  // const isDarkMode = definedColorMode === 'dark';
  return (
    <NavbarContentLayout
      left={
        // TODO stop hardcoding items?
        <>
          {!mobileSidebar.disabled && <NavbarMobileSidebarToggle />}
          <NavbarLogo />
          <div className="pr-6">
            <DocsLogoLight />
          </div>
          <div className={styles.dividerLine}>|</div>
          <NavbarItems items={leftItems} />
          <div className="">
            <NavbarColorModeToggle className={styles.colorModeToggle} />
          </div>
        </>
      }
      right={
        // TODO stop hardcoding items?
        // Ask the user to add the respective navbar items => more flexible
        <>
          <a className="navbar__item navbar__link flex">
            <BrowserOnly fallback={<div>Loading...</div>}>{() => <AiChatBot />}</BrowserOnly>
          </a>
          <NavbarItems items={rightItems} />
          <CopyLLM />
        </>
      }
    />
  );
}
