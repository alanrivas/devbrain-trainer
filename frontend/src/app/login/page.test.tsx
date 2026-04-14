import { render, screen } from '@testing-library/react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/components/AuthContext';
import LoginPage from './page';

// Mock dependencies
jest.mock('next/navigation');
jest.mock('@/components/AuthContext');
jest.mock('@/components/LoginForm', () => {
  return function MockLoginForm() {
    return <form data-testid="login-form"></form>;
  };
});

describe('LoginPage', () => {
  const mockPush = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
  });

  describe('Rendering', () => {
    it('should render login form', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      render(<LoginPage />);
      expect(screen.getByTestId('login-form')).toBeInTheDocument();
    });

    it('should render page title', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      render(<LoginPage />);
      expect(screen.getByText('DevBrain Trainer')).toBeInTheDocument();
    });

    it('should render subtitle', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      render(<LoginPage />);
      expect(screen.getByText(/train your mind/i)).toBeInTheDocument();
    });

    it('should have gradient background', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      const { container } = render(<LoginPage />);
      const bgDiv = container.querySelector('.bg-gradient-to-br');
      expect(bgDiv).toBeInTheDocument();
    });
  });

  describe('Authentication Redirect', () => {
    it('should redirect to challenges if already authenticated', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: true });
      
      render(<LoginPage />);
      
      // useEffect with redirect
      setTimeout(() => {
        expect(mockPush).toHaveBeenCalledWith('/challenges');
      }, 0);
    });

    it('should not redirect if not authenticated', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      render(<LoginPage />);
      
      expect(mockPush).not.toHaveBeenCalled();
    });
  });

  describe('Layout', () => {
    it('should use light color scheme', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      const { container } = render(<LoginPage />);
      const mainDiv = container.firstChild;
      expect(mainDiv).toHaveClass('min-h-screen');
      expect(mainDiv).toHaveClass('bg-gradient-to-br');
    });

    it('should center content', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      const { container } = render(<LoginPage />);
      const mainDiv = container.firstChild;
      expect(mainDiv).toHaveClass('flex');
      expect(mainDiv).toHaveClass('items-center');
      expect(mainDiv).toHaveClass('justify-center');
    });

    it('should have responsive padding', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      const { container } = render(<LoginPage />);
      const mainDiv = container.firstChild;
      expect(mainDiv).toHaveClass('px-4');
      expect(mainDiv).toHaveClass('py-12');
    });
  });

  describe('Accessibility', () => {
    it('should have semantic HTML', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      const { container } = render(<LoginPage />);
      expect(container.querySelector('h1')).toBeInTheDocument();
      expect(container.querySelector('p')).toBeInTheDocument();
    });
  });
});
