'use client';

import Link from 'next/link';
import { useAuth } from '@/components/AuthContext';

export default function Home() {
  const { isAuthenticated } = useAuth();

  return (
    <div className="flex flex-col flex-1 items-center justify-center bg-gradient-to-b from-blue-50 to-white">
      <main className="w-full max-w-4xl px-6 py-32 text-center">
        <h1 className="text-5xl font-bold text-gray-900 mb-6">
          DevBrain Trainer
        </h1>
        <p className="text-xl text-gray-600 mb-8">
          Cognitive training platform for developers
        </p>
        <p className="text-lg text-gray-500 mb-12 max-w-2xl mx-auto">
          Improve your logic, memory, and reasoning with real-world tech problems.
          Track your progress with ELO ratings and badges.
        </p>

        {isAuthenticated ? (
          <div className="flex gap-4 justify-center">
            <Link
              href="/challenges"
              className="px-8 py-3 text-lg font-semibold text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition"
            >
              Start Challenges
            </Link>
            <Link
              href="/stats"
              className="px-8 py-3 text-lg font-semibold text-blue-600 border-2 border-blue-600 rounded-lg hover:bg-blue-50 transition"
            >
              View Stats
            </Link>
          </div>
        ) : (
          <div className="flex gap-4 justify-center">
            <Link
              href="/register"
              className="px-8 py-3 text-lg font-semibold text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition"
            >
              Get Started
            </Link>
            <Link
              href="/login"
              className="px-8 py-3 text-lg font-semibold text-blue-600 border-2 border-blue-600 rounded-lg hover:bg-blue-50 transition"
            >
              Sign In
            </Link>
          </div>
        )}

        <div className="mt-16 grid grid-cols-1 md:grid-cols-3 gap-8 text-left">
          <div className="p-6 bg-white rounded-lg shadow-sm">
            <h3 className="text-xl font-semibold text-gray-900 mb-2">🧠 Train Your Mind</h3>
            <p className="text-gray-600">
              Solve real-world tech problems to improve logic and reasoning skills.
            </p>
          </div>
          <div className="p-6 bg-white rounded-lg shadow-sm">
            <h3 className="text-xl font-semibold text-gray-900 mb-2">📊 Track Progress</h3>
            <p className="text-gray-600">
              Monitor your improvement with ELO ratings, streaks, and badges.
            </p>
          </div>
          <div className="p-6 bg-white rounded-lg shadow-sm">
            <h3 className="text-xl font-semibold text-gray-900 mb-2">🏆 Compete</h3>
            <p className="text-gray-600">
              Challenge yourself on the leaderboard and earn exclusive achievements.
            </p>
          </div>
        </div>
      </main>
    </div>
  );
}
