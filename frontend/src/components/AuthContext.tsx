'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User, getToken, getUser, login, logout } from '@/lib/auth';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  setAuth: (token: string, user: User) => void;
  clearAuth: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Cargar token y usuario al montar el componente (lado del cliente)
  useEffect(() => {
    const storedToken = getToken();
    const storedUser = getUser();
    setToken(storedToken);
    setUser(storedUser);
    setIsLoading(false);
  }, []);

  const handleSetAuth = (newToken: string, newUser: User) => {
    login(newToken, newUser);
    setToken(newToken);
    setUser(newUser);
  };

  const handleClearAuth = () => {
    logout();
    setToken(null);
    setUser(null);
  };

  const value: AuthContextType = {
    user,
    token,
    isLoading,
    isAuthenticated: token !== null,
    setAuth: handleSetAuth,
    clearAuth: handleClearAuth,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth debe ser usado dentro de AuthProvider');
  }
  return context;
};
