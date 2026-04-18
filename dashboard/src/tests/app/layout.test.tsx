import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import RootLayout from '@/app/layout';

// Mock the AppProviders component with more detailed structure
jest.mock('@/hoc/AppProviders', () => {
  return function MockAppProviders({ children }: { children: React.ReactNode }) {
    return (
      <div data-testid="app-providers">
        <div data-testid="store-provider">
          <div data-testid="app-context-provider">
            <div data-testid="style-provider">
              <div data-testid="antd-registry">
                <div data-testid="config-provider">
                  <div data-testid="auth-layout">{children}</div>
                  <div data-testid="toaster" />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  };
});

// Mock Next.js font
jest.mock('next/font/google', () => ({
  Inter: jest.fn(() => ({
    className: 'inter-font-class',
    style: { fontFamily: 'Inter' },
  })),
}));

// Mock the global CSS import
jest.mock('@/assets/css/globals.scss', () => ({}));

const getRootLayoutElement = (children: React.ReactNode = <div>Test children</div>) =>
  RootLayout({ children }) as React.ReactElement<any>;

const getDocumentParts = (children?: React.ReactNode) => {
  const html = getRootLayoutElement(children);
  const htmlChildren = React.Children.toArray(html.props.children) as React.ReactElement<any>[];
  const head = htmlChildren.find((child) => React.isValidElement(child) && child.type === 'head');
  const body = htmlChildren.find((child) => React.isValidElement(child) && child.type === 'body');

  if (!head || !body) {
    throw new Error('RootLayout did not return the expected document structure.');
  }

  return { html, head, body };
};

const getHeadLinks = (head: React.ReactElement<any>) =>
  React.Children.toArray(head.props.children).filter(
    (child): child is React.ReactElement<any> => React.isValidElement(child) && child.type === 'link'
  );

describe('RootLayout Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = render(
      <RootLayout>
        <div>Test children</div>
      </RootLayout>
    );

    expect(container).toBeInTheDocument();
  });

  it('renders html element with correct lang attribute', () => {
    const { html } = getDocumentParts();
    expect(html.type).toBe('html');
    expect(html.props.lang).toBe('en');
  });

  it('renders head element with favicon links', () => {
    const { head } = getDocumentParts();
    const headLinks = getHeadLinks(head);

    const faviconLink = headLinks.find((link) => link.props.rel === 'icon');
    expect(head.type).toBe('head');
    expect(faviconLink?.props.href).toBe('/favicon.png');
    expect(faviconLink?.props.sizes).toBe('any');
  });

  it('renders apple touch icon link', () => {
    const { head } = getDocumentParts();
    const appleTouchIcon = getHeadLinks(head).find((link) => link.props.rel === 'apple-touch-icon');

    expect(appleTouchIcon?.props.href).toBe('/bombe-icon-3x.png?<generated>');
    expect(appleTouchIcon?.props.type).toBe('image/<generated>');
    expect(appleTouchIcon?.props.sizes).toBe('<generated>');
  });

  it('renders Google Fonts preconnect links', () => {
    const { head } = getDocumentParts();
    const preconnectLinks = getHeadLinks(head).filter((link) => link.props.rel === 'preconnect');

    expect(preconnectLinks).toHaveLength(2);

    const googleFontsLink = preconnectLinks.find((link) => link.props.href === 'https://fonts.googleapis.com');
    expect(googleFontsLink).toBeDefined();

    const googleStaticLink = preconnectLinks.find((link) => link.props.href === 'https://fonts.gstatic.com');
    expect(googleStaticLink?.props.crossOrigin).toBe('anonymous');
  });

  it('renders body element with Inter font class', () => {
    const { body } = getDocumentParts();
    expect(body.type).toBe('body');
    expect(body.props.className).toBe('inter-font-class');
  });

  it('renders AppProviders wrapper with complete provider hierarchy', () => {
    const { getByTestId } = render(
      <RootLayout>
        <div>Test children</div>
      </RootLayout>
    );

    expect(getByTestId('app-providers')).toBeInTheDocument();
    expect(getByTestId('store-provider')).toBeInTheDocument();
    expect(getByTestId('app-context-provider')).toBeInTheDocument();
    expect(getByTestId('style-provider')).toBeInTheDocument();
    expect(getByTestId('antd-registry')).toBeInTheDocument();
    expect(getByTestId('config-provider')).toBeInTheDocument();
    expect(getByTestId('auth-layout')).toBeInTheDocument();
    expect(getByTestId('toaster')).toBeInTheDocument();
  });

  it('renders children inside AppProviders hierarchy', () => {
    const { getByText, getByTestId } = render(
      <RootLayout>
        <div>Test children content</div>
      </RootLayout>
    );

    const appProviders = getByTestId('app-providers');
    const authLayout = getByTestId('auth-layout');

    expect(appProviders).toContainElement(getByText('Test children content'));
    expect(authLayout).toContainElement(getByText('Test children content'));
  });

  it('maintains proper provider nesting order', () => {
    const { getByTestId } = render(
      <RootLayout>
        <div>Test children</div>
      </RootLayout>
    );

    const appProviders = getByTestId('app-providers');
    const storeProvider = getByTestId('store-provider');
    const appContextProvider = getByTestId('app-context-provider');
    const styleProvider = getByTestId('style-provider');
    const antdRegistry = getByTestId('antd-registry');
    const configProvider = getByTestId('config-provider');
    const authLayout = getByTestId('auth-layout');

    // Check nesting hierarchy
    expect(appProviders).toContainElement(storeProvider);
    expect(storeProvider).toContainElement(appContextProvider);
    expect(appContextProvider).toContainElement(styleProvider);
    expect(styleProvider).toContainElement(antdRegistry);
    expect(antdRegistry).toContainElement(configProvider);
    expect(configProvider).toContainElement(authLayout);
  });

  it('renders multiple children', () => {
    const { getByText } = render(
      <RootLayout>
        <div>First child</div>
        <div>Second child</div>
        <div>Third child</div>
      </RootLayout>
    );

    expect(getByText('First child')).toBeInTheDocument();
    expect(getByText('Second child')).toBeInTheDocument();
    expect(getByText('Third child')).toBeInTheDocument();
  });

  it('renders complex nested children', () => {
    const { getByText } = render(
      <RootLayout>
        <div>
          <h1>Title</h1>
          <p>
            Paragraph with <strong>bold text</strong>
          </p>
        </div>
      </RootLayout>
    );

    expect(getByText('Title')).toBeInTheDocument();
    expect(getByText('Paragraph with')).toBeInTheDocument();
    expect(getByText('bold text')).toBeInTheDocument();
  });

  it('renders empty children', () => {
    const { container } = render(<RootLayout>{null}</RootLayout>);
    const { html } = getDocumentParts(null);

    expect(container).toBeInTheDocument();
    expect(html.type).toBe('html');
  });

  it('renders with React fragments as children', () => {
    const { getByText } = render(
      <RootLayout>
        <>
          <div>Fragment child 1</div>
          <div>Fragment child 2</div>
        </>
      </RootLayout>
    );

    expect(getByText('Fragment child 1')).toBeInTheDocument();
    expect(getByText('Fragment child 2')).toBeInTheDocument();
  });

  it('maintains proper HTML structure', () => {
    const { html, head, body } = getDocumentParts(<div>Test content</div>);

    expect(html.type).toBe('html');
    expect(head.type).toBe('head');
    expect(body.type).toBe('body');
    expect(React.Children.toArray(html.props.children)).toEqual(expect.arrayContaining([head, body]));
  });

  it('applies correct font styling', () => {
    const { body } = getDocumentParts(<div>Test content</div>);
    expect(body.props.className).toBe('inter-font-class');
  });

  it('renders Toaster component for notifications', () => {
    const { getByTestId } = render(
      <RootLayout>
        <div>Test content</div>
      </RootLayout>
    );

    expect(getByTestId('toaster')).toBeInTheDocument();
  });

  it('renders AuthLayout wrapper for authentication context', () => {
    const { getByTestId } = render(
      <RootLayout>
        <div>Test content</div>
      </RootLayout>
    );

    expect(getByTestId('auth-layout')).toBeInTheDocument();
  });

  it('provides complete application context through providers', () => {
    const { getByTestId } = render(
      <RootLayout>
        <div>Test content</div>
      </RootLayout>
    );

    // Verify all essential providers are present
    expect(getByTestId('store-provider')).toBeInTheDocument();
    expect(getByTestId('app-context-provider')).toBeInTheDocument();
    expect(getByTestId('style-provider')).toBeInTheDocument();
    expect(getByTestId('antd-registry')).toBeInTheDocument();
    expect(getByTestId('config-provider')).toBeInTheDocument();
  });

  it('maintains provider structure across re-renders', () => {
    const { getByTestId, rerender } = render(
      <RootLayout>
        <div>Initial content</div>
      </RootLayout>
    );

    const initialProviders = getByTestId('app-providers');
    expect(initialProviders).toBeInTheDocument();

    // Re-render with different content
    rerender(
      <RootLayout>
        <div>Updated content</div>
      </RootLayout>
    );

    const updatedProviders = getByTestId('app-providers');
    expect(updatedProviders).toBeInTheDocument();
    expect(updatedProviders).toBe(initialProviders);
  });

  it('handles complex nested content within provider hierarchy', () => {
    const { getByText, getByTestId } = render(
      <RootLayout>
        <div>
          <header>Header</header>
          <main>
            <section>Section 1</section>
            <section>Section 2</section>
          </main>
          <footer>Footer</footer>
        </div>
      </RootLayout>
    );

    const authLayout = getByTestId('auth-layout');

    expect(authLayout).toContainElement(getByText('Header'));
    expect(authLayout).toContainElement(getByText('Section 1'));
    expect(authLayout).toContainElement(getByText('Section 2'));
    expect(authLayout).toContainElement(getByText('Footer'));
  });

  it('ensures children are rendered at the correct level in provider hierarchy', () => {
    const { getByText, getByTestId } = render(
      <RootLayout>
        <div>Test children</div>
      </RootLayout>
    );

    const appProviders = getByTestId('app-providers');
    const authLayout = getByTestId('auth-layout');
    const childrenElement = getByText('Test children');

    // Children should be inside auth-layout but not directly in app-providers
    expect(authLayout).toContainElement(childrenElement);
    expect(appProviders).toContainElement(childrenElement);
  });

  it('provides consistent provider structure for different content types', () => {
    const { getByTestId, rerender } = render(
      <RootLayout>
        <div>Text content</div>
      </RootLayout>
    );

    expect(getByTestId('app-providers')).toBeInTheDocument();
    expect(getByTestId('auth-layout')).toBeInTheDocument();

    // Re-render with different content type
    rerender(
      <RootLayout>
        <span>Inline content</span>
      </RootLayout>
    );

    expect(getByTestId('app-providers')).toBeInTheDocument();
    expect(getByTestId('auth-layout')).toBeInTheDocument();
  });

  it('maintains HTML document structure with provider integration', () => {
    const { container } = render(
      <RootLayout>
        <div>Test content</div>
      </RootLayout>
    );
    const { html, head, body } = getDocumentParts(<div>Test content</div>);

    const appProviders = container.querySelector('[data-testid="app-providers"]');

    expect(html.type).toBe('html');
    expect(head.type).toBe('head');
    expect(body.type).toBe('body');
    expect(appProviders).toBeInTheDocument();
    expect(body.props.children.type.name).toBe('MockAppProviders');
  });
});
