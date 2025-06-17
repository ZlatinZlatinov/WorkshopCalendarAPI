import React, { useState } from 'react';
import { login, register } from '../api';

export default function LoginPage({ onAuth }: { onAuth: (token: string) => void }) {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [name, setName] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const validate = () => {
    if (!email.match(/^[^@\s]+@[^@\s]+\.[^@\s]+$/)) return 'Please enter a valid email address.';
    if (password.length < 6) return 'Password must be at least 6 characters.';
    if (!isLogin && name.trim().length < 2) return 'Name must be at least 2 characters.';
    return '';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    const validation = validate();
    if (validation) {
      setError(validation);
      return;
    }
    setLoading(true);
    try {
      let result;
      if (isLogin) {
        result = await login(email, password);
      } else {
        result = await register(name, email, password);
      }
      if (result.token) {
        localStorage.setItem('token', result.token);
        onAuth(result.token);
      } else {
        setError(result.message || (isLogin ? 'Login failed. Please check your credentials.' : 'Registration failed. Try a different email.'));
      }
    } catch (err) {
      setError('Network error. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-100">
      <form onSubmit={handleSubmit} className="bg-white p-6 rounded shadow w-80 flex flex-col gap-2" aria-label={isLogin ? 'Login form' : 'Register form'}>
        <h2 className="text-xl font-bold mb-2 text-center">{isLogin ? 'Login' : 'Register'}</h2>
        {!isLogin && (
          <label className="flex flex-col text-sm font-medium" htmlFor="name">
            Name
            <input
              id="name"
              className="mt-1 mb-2 w-full p-2 border rounded"
              placeholder="Name"
              value={name}
              onChange={e => setName(e.target.value)}
              required
              minLength={2}
              aria-required={!isLogin}
            />
          </label>
        )}
        <label className="flex flex-col text-sm font-medium" htmlFor="email">
          Email
          <input
            id="email"
            className="mt-1 mb-2 w-full p-2 border rounded"
            placeholder="Email"
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
            aria-required="true"
          />
        </label>
        <label className="flex flex-col text-sm font-medium" htmlFor="password">
          Password
          <input
            id="password"
            className="mt-1 mb-2 w-full p-2 border rounded"
            placeholder="Password"
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
            minLength={6}
            aria-required="true"
          />
        </label>
        {error && <div className="text-red-500 mb-2" role="alert">{error}</div>}
        <button className="w-full bg-blue-600 text-white p-2 rounded mb-2 disabled:opacity-60" type="submit" disabled={loading} aria-busy={loading}>
          {loading ? (isLogin ? 'Logging in...' : 'Registering...') : (isLogin ? 'Login' : 'Register')}
        </button>
        <button
          type="button"
          className="w-full text-blue-600 underline"
          onClick={() => setIsLogin(l => !l)}
        >
          {isLogin ? 'Need an account? Register' : 'Already have an account? Login'}
        </button>
      </form>
    </div>
  );
} 