'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/components/AuthContext';
import { useRouter } from 'next/navigation';
import ChallengeCard from '@/components/ChallengeCard';
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
        sessionStorage.setItem('challenge-list-ids', JSON.stringify(response.data.items.map((c) => c.id)));
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
            <p className="text-gray-700">No challenges available</p>
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {challenges.map((challenge) => (
              <ChallengeCard 
                key={challenge.id} 
                challenge={challenge}
                onAttempt={(challengeId) => {
                  router.push(`/challenges/${challengeId}`);
                }}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
