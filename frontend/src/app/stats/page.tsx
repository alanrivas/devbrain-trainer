'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/components/AuthContext';
import api from '@/lib/api';

interface UserStats {
  userId: string;
  displayName: string;
  totalAttempts: number;
  correctAttempts: number;
  accuracyRate: number;
  currentStreak: number;
  eloRating: number;
  lastAttemptAt: string | null;
}

interface UserBadge {
  type: string;
  earnedAt: string;
}

export default function StatsPage() {
  const router = useRouter();
  const { token, clearAuth } = useAuth();
  const [stats, setStats] = useState<UserStats | null>(null);
  const [badges, setBadges] = useState<UserBadge[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!token) {
      router.push('/login');
      return;
    }

    const fetchData = async () => {
      try {
        const [statsResponse, badgesResponse] = await Promise.all([
          api.get<UserStats>('/users/me/stats'),
          api.get<UserBadge[]>('/users/me/badges'),
        ]);
        setStats(statsResponse.data);
        setBadges(badgesResponse.data);
      } catch (err: any) {
        const status = err?.response?.status;
        if (status === 401) {
          clearAuth();
          router.push('/login');
          return;
        }
        setError('Failed to load stats.');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [token, router, clearAuth]);

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <p className="text-gray-600">Loading stats...</p>
      </div>
    );
  }

  if (error || !stats) {
    return (
      <main className="min-h-screen bg-gray-50 py-12 px-4">
        <div className="max-w-3xl mx-auto bg-white rounded-lg shadow p-8">
          <div role="alert" className="p-3 rounded border border-red-200 bg-red-50 text-red-700 text-sm">
            {error || 'Failed to load stats.'}
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-3xl mx-auto space-y-6">
        <h1 className="text-3xl font-bold text-gray-900">{stats.displayName}</h1>

        <section className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Stats</h2>
          <dl className="grid grid-cols-2 gap-4">
            <div>
              <dt className="text-sm text-gray-500">Total Attempts</dt>
              <dd className="text-2xl font-bold text-gray-900">{stats.totalAttempts}</dd>
            </div>
            <div>
              <dt className="text-sm text-gray-500">Correct Attempts</dt>
              <dd className="text-2xl font-bold text-gray-900">{stats.correctAttempts}</dd>
            </div>
            <div>
              <dt className="text-sm text-gray-500">Accuracy</dt>
              <dd className="text-2xl font-bold text-gray-900">{stats.accuracyRate.toFixed(1)}%</dd>
            </div>
            <div>
              <dt className="text-sm text-gray-500">Current Streak</dt>
              <dd className="text-2xl font-bold text-gray-900">{stats.currentStreak} days</dd>
            </div>
            <div>
              <dt className="text-sm text-gray-500">ELO Rating</dt>
              <dd className="text-2xl font-bold text-gray-900">{stats.eloRating}</dd>
            </div>
            <div>
              <dt className="text-sm text-gray-500">Last Attempt</dt>
              <dd className="text-2xl font-bold text-gray-900">
                {stats.lastAttemptAt
                  ? new Date(stats.lastAttemptAt).toLocaleDateString()
                  : 'Never'}
              </dd>
            </div>
          </dl>
        </section>

        <section className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Badges</h2>
          {badges.length === 0 ? (
            <p className="text-gray-500">No badges earned yet</p>
          ) : (
            <ul className="space-y-2">
              {badges.map((badge, idx) => (
                <li key={idx} className="flex items-center justify-between text-sm">
                  <span className="font-medium text-gray-900">{badge.type}</span>
                  <span className="text-gray-500">{new Date(badge.earnedAt).toLocaleDateString()}</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </main>
  );
}
