'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/components/AuthContext';
import { useRouter } from 'next/navigation';
import api from '@/lib/api';

interface Challenge {
  id: string;
  title: string;
  description: string;
  category: string;
  difficulty: string;
}

interface PagedResult {
  items: Challenge[];
  page: number;
  pageSize: number;
  total: number;
}

export default function ChallengesPage() {
  const { user, token, clearAuth } = useAuth();
  const router = useRouter();
  const [challenges, setChallenges] = useState<Challenge[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    // Redirect if not authenticated
    if (!token) {
      router.push('/login');
      return;
    }

    // Fetch challenges
    const fetchChallenges = async () => {
      try {
        const response = await api.get<PagedResult>('/challenges?page=1&pageSize=10');
        setChallenges(response.data.items);
      } catch (err: any) {
        if (err.response?.status === 401) {
          clearAuth();
          router.push('/login');
        } else {
          setError('Failed to load challenges');
        }
      } finally {
        setLoading(false);
      }
    };

    fetchChallenges();
  }, [token, router, clearAuth]);

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading challenges...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4">
      <div className="max-w-6xl mx-auto">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900">Challenges</h1>
          <p className="text-gray-600 mt-2">Train your skills with real-world coding problems</p>
        </div>

        {error && (
          <div className="mb-4 p-4 bg-red-50 border border-red-200 text-red-700 rounded">
            {error}
          </div>
        )}

        {challenges.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-gray-500">No challenges available</p>
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {challenges.map((challenge) => (
              <div key={challenge.id} className="bg-white rounded-lg shadow p-6 hover:shadow-lg transition">
                <div className="flex items-start justify-between mb-4">
                  <h3 className="text-lg font-semibold text-gray-900 flex-1">{challenge.title}</h3>
                  <span className={`px-3 py-1 rounded-full text-sm font-medium ${
                    challenge.difficulty === 'Easy'
                      ? 'bg-green-100 text-green-800'
                      : challenge.difficulty === 'Medium'
                      ? 'bg-yellow-100 text-yellow-800'
                      : 'bg-red-100 text-red-800'
                  }`}>
                    {challenge.difficulty}
                  </span>
                </div>
                <p className="text-gray-600 text-sm mb-4">{challenge.description}</p>
                <div className="flex items-center justify-between">
                  <span className="text-xs bg-gray-100 text-gray-700 px-2 py-1 rounded">
                    {challenge.category}
                  </span>
                  <button className="text-blue-600 hover:text-blue-700 font-medium text-sm">
                    Attempt →
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
