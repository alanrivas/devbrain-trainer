'use client';

import React from 'react';
import Link from 'next/link';
import { useAuth } from './AuthContext';

export const Header: React.FC = () => {
  const { isAuthenticated, user, clearAuth } = useAuth();

  const handleLogout = () => {
    clearAuth();
    // Opcional: redirigir a home después de logout
    window.location.href = '/';
  };

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/60">
      <div className="container flex h-14 max-w-screen-2xl items-center justify-between px-4">
        {/* Logo */}
        <Link href="/" className="flex items-center gap-2">
          <span className="text-xl font-bold text-blue-600">DevBrain</span>
          <span className="text-sm text-gray-600">Trainer</span>
        </Link>

        {/* Navigation */}
        <nav className="hidden md:flex items-center gap-8">
          <Link href="/" className="text-sm font-medium text-gray-700 hover:text-gray-900">
            Home
          </Link>
          {isAuthenticated && (
            <>
              <Link href="/challenges" className="text-sm font-medium text-gray-700 hover:text-gray-900">
                Challenges
              </Link>
              <Link href="/stats" className="text-sm font-medium text-gray-700 hover:text-gray-900">
                Stats
              </Link>
            </>
          )}
        </nav>

        {/* Auth Controls */}
        <div className="flex items-center gap-4">
          {isAuthenticated && user ? (
            <>
              <span className="text-sm text-gray-700">{user.email}</span>
              <button
                onClick={handleLogout}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-md hover:bg-red-700"
              >
                Logout
              </button>
            </>
          ) : (
            <>
              <Link href="/login" className="px-4 py-2 text-sm font-medium text-blue-600 hover:text-blue-700">
                Sign In
              </Link>
              <Link href="/register" className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700">
                Sign Up
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
};
