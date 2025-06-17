import React, { useEffect, useState } from 'react';
import { getUsers } from '../api';

export default function UsersPage({ onLogout, onGoToEvents }: { onLogout: () => void, onGoToEvents: () => void }) {
  const [users, setUsers] = useState<any[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) return;
    setLoading(true);
    getUsers(token)
      .then(setUsers)
      .catch(() => setError('Failed to load users. Please try again later.'))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-100">
      <div className="p-6 max-w-2xl w-full mx-auto bg-white rounded shadow">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-3xl font-bold text-gray-800">Users</h1>
          <button className="text-red-600 underline font-semibold" onClick={onLogout}>Logout</button>
        </div>
        <button className="mb-6 bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded font-semibold" onClick={onGoToEvents} aria-label="Go to Events">Go to Events</button>
        {loading ? (
          <div className="text-gray-600 text-center">Loading users...</div>
        ) : error ? (
          <div className="text-red-500 mb-2" role="alert">{error}</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full border border-gray-200 rounded">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-2 text-left text-gray-700 font-semibold">Name</th>
                  <th className="px-4 py-2 text-left text-gray-700 font-semibold">Email</th>
                </tr>
              </thead>
              <tbody>
                {users.map(u => (
                  <tr key={u.id} className="even:bg-gray-50 hover:bg-blue-50 transition">
                    <td className="px-4 py-2 font-medium text-gray-900">{u.name}</td>
                    <td className="px-4 py-2 text-gray-700">{u.email}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
} 