import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRouter } from 'next/navigation';
import LoginForm from './LoginForm';
import { useAuth } from './AuthContext';
import api from '@/lib/api';

// Mock dependencies
jest.mock('next/navigation');
jest.mock('./AuthContext');
jest.mock('@/lib/api');

describe('LoginForm', () => {
  const mockPush = jest.fn();
  const mockSetAuth = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (useAuth as jest.Mock).mockReturnValue({ setAuth: mockSetAuth });
  });

  describe('Rendering', () => {
    it('should render email and password inputs', () => {
      render(<LoginForm />);
      expect(screen.getByPlaceholderText(/you@example.com/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/••••••••/i)).toBeInTheDocument();
    });

    it('should render submit button', () => {
      render(<LoginForm />);
      expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
    });

    it('should render sign up link', () => {
      render(<LoginForm />);
      expect(screen.getByRole('link', { name: /sign up/i })).toBeInTheDocument();
    });

    it('should render form title', () => {
      render(<LoginForm />);
      const heading = screen.getByRole('heading');
      expect(heading).toHaveTextContent(/sign in/i);
    });

    it('should have accessible labels', () => {
      render(<LoginForm />);
      expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    });
  });

  describe('Validation', () => {
    it('should not submit empty form', async () => {
      render(<LoginForm />);
      const submitButton = screen.getByRole('button', { name: /sign in/i });
      
      fireEvent.click(submitButton);
      
      expect(api.post).not.toHaveBeenCalled();
    });

    it('should reject invalid email format', async () => {
      render(<LoginForm />);
      const emailInput = screen.getByPlaceholderText(/you@example.com/i);
      
      await userEvent.type(emailInput, 'invalid-email');
      fireEvent.click(screen.getByRole('button', { name: /sign in/i }));
      
      await waitFor(() => {
        expect(api.post).not.toHaveBeenCalled();
      });
    });

    it('should reject short passwords', async () => {
      render(<LoginForm />);
      const emailInput = screen.getByPlaceholderText(/you@example.com/i);
      const passwordInput = screen.getByPlaceholderText(/••••••••/i);
      
      await userEvent.type(emailInput, 'test@example.com');
      await userEvent.type(passwordInput, 'ab');
      fireEvent.click(screen.getByRole('button', { name: /sign in/i }));
      
      await waitFor(() => {
        expect(api.post).not.toHaveBeenCalled();
      });
    });
  });

  describe('Submission', () => {
    it('should submit form with valid credentials', async () => {
      (api.post as jest.Mock).mockResolvedValue({
        data: { token: 'fake-token', user: { id: '1', email: 'test@example.com' } },
      });

      render(<LoginForm />);
      const emailInput = screen.getByPlaceholderText(/you@example.com/i);
      const passwordInput = screen.getByPlaceholderText(/••••••••/i);
      
      await userEvent.type(emailInput, 'test@example.com');
      await userEvent.type(passwordInput, 'Password123');
      fireEvent.click(screen.getByRole('button', { name: /sign in/i }));
      
      await waitFor(() => {
        expect(api.post).toHaveBeenCalledWith('/auth/login', {
          email: 'test@example.com',
          password: 'Password123',
        });
      });
    });

    it('should set auth and redirect on success', async () => {
      const fakeToken = 'fake-token';
      const fakeUser = { id: '1', email: 'test@example.com', name: 'Test User' };
      
      (api.post as jest.Mock).mockResolvedValue({
        data: { token: fakeToken, user: fakeUser },
      });

      render(<LoginForm />);
      const emailInput = screen.getByPlaceholderText(/you@example.com/i);
      const passwordInput = screen.getByPlaceholderText(/••••••••/i);
      
      await userEvent.type(emailInput, 'test@example.com');
      await userEvent.type(passwordInput, 'Password123');
      fireEvent.click(screen.getByRole('button', { name: /sign in/i }));
      
      await waitFor(() => {
        expect(mockSetAuth).toHaveBeenCalledWith(fakeToken, fakeUser);
        expect(mockPush).toHaveBeenCalledWith('/challenges');
      });
    });

    it('should disable button while loading', async () => {
      (api.post as jest.Mock).mockImplementation(
        () => new Promise((resolve) => setTimeout(() => resolve({
          data: { token: 'fake-token', user: { id: '1' } },
        }), 100))
      );

      render(<LoginForm />);
      const emailInput = screen.getByPlaceholderText(/you@example.com/i);
      const passwordInput = screen.getByPlaceholderText(/••••••••/i);
      const submitButton = screen.getByRole('button', { name: /sign in/i });
      
      await userEvent.type(emailInput, 'test@example.com');
      await userEvent.type(passwordInput, 'Password123');
      fireEvent.click(submitButton);
      
      expect(submitButton).toBeDisabled();
    });
  });

  describe('Error Handling', () => {
    it('should display error on login failure', async () => {
      const errorMessage = 'Invalid credentials';
      (api.post as jest.Mock).mockRejectedValue({
        response: { data: { message: errorMessage } },
      });

      render(<LoginForm />);
      const emailInput = screen.getByPlaceholderText(/you@example.com/i);
      const passwordInput = screen.getByPlaceholderText(/••••••••/i);
      
      await userEvent.type(emailInput, 'test@example.com');
      await userEvent.type(passwordInput, 'Password123');
      fireEvent.click(screen.getByRole('button', { name: /sign in/i }));
      
      await waitFor(() => {
        expect(screen.getByText(errorMessage)).toBeInTheDocument();
      });
    });

    it('should clear error on retry', async () => {
      (api.post as jest.Mock)
        .mockRejectedValueOnce({
          response: { data: { message: 'Invalid credentials' } },
        })
        .mockResolvedValueOnce({
          data: { token: 'fake-token', user: { id: '1' } },
        });

      render(<LoginForm />);
      const emailInput = screen.getByPlaceholderText(/you@example.com/i);
      const passwordInput = screen.getByPlaceholderText(/••••••••/i);
      const submitButton = screen.getByRole('button', { name: /sign in/i });
      
      // First attempt (fails)
      await userEvent.type(emailInput, 'test@example.com');
      await userEvent.type(passwordInput, 'Password123');
      fireEvent.click(submitButton);
      
      await waitFor(() => {
        expect(screen.getByText('Invalid credentials')).toBeInTheDocument();
      });

      // Second attempt (succeeds)
      await userEvent.clear(emailInput);
      await userEvent.clear(passwordInput);
      await userEvent.type(emailInput, 'test@example.com');
      await userEvent.type(passwordInput, 'Password123');
      fireEvent.click(submitButton);
      
      await waitFor(() => {
        expect(screen.queryByText('Invalid credentials')).not.toBeInTheDocument();
      });
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA labels', () => {
      render(<LoginForm />);
      expect(screen.getByLabelText(/email/i)).toHaveAttribute('type', 'email');
      expect(screen.getByLabelText(/password/i)).toHaveAttribute('type', 'password');
    });

    it('should be keyboard navigable', async () => {
      render(<LoginForm />);
      const emailInput = screen.getByPlaceholderText(/you@example.com/i);
      const submitButton = screen.getByRole('button', { name: /sign in/i });
      
      await userEvent.tab();
      expect(emailInput).toHaveFocus();
      
      await userEvent.tab();
      const passwordInput = screen.getByPlaceholderText(/••••••••/i);
      expect(passwordInput).toHaveFocus();
      
      await userEvent.tab();
      expect(submitButton).toHaveFocus();
    });
  });
});
