import { render, screen } from '@testing-library/react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/components/AuthContext';
import RegisterPage from './page';

// Mock dependencies
jest.mock('next/navigation');
jest.mock('@/components/AuthContext');
jest.mock('@/components/RegisterForm', () => {
  return function MockRegisterForm() {
    return <form data-testid="register-form"></form>;
  };
});

describe('RegisterPage', () => {
  const mockPush = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
  });

  describe('Rendering', () => {
    it('Should render RegisterForm component', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      render(<RegisterPage />);
      expect(screen.getByTestId('register-form')).toBeInTheDocument();
    });

    it('Should render page title', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      render(<RegisterPage />);
      expect(screen.getByText(/DevBrain Trainer/)).toBeInTheDocument();
    });

    it('Should render page subtitle', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      render(<RegisterPage />);
      expect(screen.getByText(/Train your mind/i)).toBeInTheDocument();
    });

    it('Should render main layout container', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      const { container } = render(<RegisterPage />);
      expect(container.querySelector('.min-h-screen')).toBeInTheDocument();
    });

    it('Should render with responsive design', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      const { container } = render(<RegisterPage />);
      expect(container.querySelector('.px-4')).toBeInTheDocument();
    });

    it('Should render gradient background', () => {
      (useAuth as jest.Mock).mockReturnValue({ isAuthenticated: false });
      
      const { container } = render(<RegisterPage />);
      expect(container.querySelector('.bg-gradient-to-br')).toBeInTheDocument();
    });
  });
});

