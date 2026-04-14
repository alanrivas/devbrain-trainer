'use client';

interface Challenge {
  id: string;
  title: string;
  description: string;
  category: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';
}

interface ChallengeCardProps {
  challenge: Challenge;
  onAttempt: (challengeId: string) => void;
}

export default function ChallengeCard({ challenge, onAttempt }: ChallengeCardProps) {
  const getDifficultyStyles = (difficulty: string) => {
    switch (difficulty) {
      case 'Easy':
        return 'bg-green-100 text-green-800';
      case 'Medium':
        return 'bg-yellow-100 text-yellow-800';
      case 'Hard':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  return (
    <div className="bg-white rounded-lg shadow p-6 hover:shadow-lg transition">
      <div className="flex items-start justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900 flex-1">{challenge.title}</h3>
        <span className={`px-3 py-1 rounded-full text-sm font-medium ${getDifficultyStyles(challenge.difficulty)}`}>
          {challenge.difficulty}
        </span>
      </div>
      <p className="text-gray-600 text-sm mb-4">{challenge.description}</p>
      <div className="flex items-center justify-between">
        <span className="text-xs bg-gray-100 text-gray-700 px-2 py-1 rounded">
          {challenge.category}
        </span>
        <button 
          onClick={() => onAttempt(challenge.id)}
          className="text-blue-600 hover:text-blue-700 font-medium text-sm"
        >
          Attempt →
        </button>
      </div>
    </div>
  );
}
