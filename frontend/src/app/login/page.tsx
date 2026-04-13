import LoginForm from '@/components/LoginForm';

export const metadata = {
  title: 'Sign In | DevBrain Trainer',
  description: 'Sign in to your DevBrain Trainer account',
};

export default function LoginPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900">DevBrain Trainer</h1>
          <p className="text-gray-600 mt-2">Train your mind. Improve your skills.</p>
        </div>
        <LoginForm />
      </div>
    </div>
  );
}
