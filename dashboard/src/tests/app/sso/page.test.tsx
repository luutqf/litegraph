import { render, screen, waitFor } from '@testing-library/react';
import { useSearchParams, useRouter } from 'next/navigation';
import SSOPage from '@/app/sso/page';
import { useApiKeyToLogin } from '@/hooks/authHooks';

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useSearchParams: jest.fn(),
  useRouter: jest.fn(),
}));

// Mock auth hooks
jest.mock('@/hooks/authHooks', () => ({
  useApiKeyToLogin: jest.fn(),
}));

// Mock toast
jest.mock('react-hot-toast', () => ({
  error: jest.fn(),
  success: jest.fn(),
}));

describe('SSOPage', () => {
  const mockLoginWithApiKey = jest.fn();
  const mockRouter = {
    push: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
    (useApiKeyToLogin as jest.Mock).mockReturnValue({
      loginWithApiKey: mockLoginWithApiKey,
      isLoading: false,
      error: null,
    });
    (useRouter as jest.Mock).mockReturnValue(mockRouter);
  });

  it('should show missing parameters message when apikey is missing', async () => {
    (useSearchParams as jest.Mock).mockReturnValue({
      get: jest.fn().mockReturnValue(null),
    });

    render(<SSOPage />);

    await waitFor(() => {
      expect(screen.getByText('Missing API Key or Endpoint.')).toBeInTheDocument();
    });
  });

  it('should show missing parameters message when endpoint is missing', async () => {
    (useSearchParams as jest.Mock).mockReturnValue({
      get: jest.fn((key) => (key === 'apikey' ? 'test-key' : null)),
    });

    render(<SSOPage />);

    await waitFor(() => {
      expect(screen.getByText('Missing API Key or Endpoint.')).toBeInTheDocument();
    });
  });

  it('should show loading state during SSO login', async () => {
    (useSearchParams as jest.Mock).mockReturnValue({
      get: jest.fn((key) => {
        if (key === 'apikey') return 'test-api-key';
        if (key === 'endpoint') return 'https://test-endpoint.com';
        return null;
      }),
    });

    (useApiKeyToLogin as jest.Mock).mockReturnValue({
      loginWithApiKey: jest.fn(() => new Promise(() => {})),
      isLoading: true,
      error: null,
    });

    render(<SSOPage />);

    await waitFor(() => {
      expect(screen.getByText('Logging in...')).toBeInTheDocument();
    });
  });

  it('should process SSO login with valid parameters', async () => {
    (useSearchParams as jest.Mock).mockReturnValue({
      get: jest.fn((key) => {
        if (key === 'apikey') return 'test-api-key';
        if (key === 'endpoint') return 'https://test-endpoint.com';
        return null;
      }),
    });

    mockLoginWithApiKey.mockResolvedValue(true);

    render(<SSOPage />);

    await waitFor(() => {
      expect(mockLoginWithApiKey).toHaveBeenCalledWith('test-api-key', 'https://test-endpoint.com');
    });
  });

  it('should show success message when SSO login succeeds', async () => {
    (useSearchParams as jest.Mock).mockReturnValue({
      get: jest.fn((key) => {
        if (key === 'apikey') return 'test-api-key';
        if (key === 'endpoint') return 'https://test-endpoint.com';
        return null;
      }),
    });

    mockLoginWithApiKey.mockResolvedValue({
      success: true,
      tenant: { GUID: 'tenant-guid' },
    });

    render(<SSOPage />);

    await waitFor(() => {
      expect(screen.getByText('Redirecting to dashboard...')).toBeInTheDocument();
    });
  });

  it('should show error message when SSO login fails', async () => {
    (useSearchParams as jest.Mock).mockReturnValue({
      get: jest.fn((key) => {
        if (key === 'apikey') return 'test-api-key';
        if (key === 'endpoint') return 'https://test-endpoint.com';
        return null;
      }),
    });

    mockLoginWithApiKey.mockResolvedValue({ success: false });

    render(<SSOPage />);

    await waitFor(() => {
      expect(screen.getByText('Cannot validate the API key.')).toBeInTheDocument();
    });
  });

  it('should handle URL encoded parameters', async () => {
    const encodedApiKey = encodeURIComponent('test+api+key');
    const encodedEndpoint = encodeURIComponent('https://test-endpoint.com');

    (useSearchParams as jest.Mock).mockReturnValue({
      get: jest.fn((key) => {
        if (key === 'apikey') return encodedApiKey;
        if (key === 'endpoint') return encodedEndpoint;
        return null;
      }),
    });

    mockLoginWithApiKey.mockResolvedValue(true);

    render(<SSOPage />);

    await waitFor(() => {
      expect(mockLoginWithApiKey).toHaveBeenCalledWith('test+api+key', 'https://test-endpoint.com');
    });
  });
});
