import React, { useEffect, useState } from 'react';
import { getEvents } from '../api';

export default function EventsPage({ onBack }: { onBack: () => void }) {
  const [events, setEvents] = useState<any[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) return;
    setLoading(true);
    getEvents(token)
      .then(setEvents)
      .catch(() => setError('Failed to load events. Please try again later.'))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-100">
      <div className="p-6 max-w-2xl w-full mx-auto bg-white rounded shadow">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-3xl font-bold text-gray-800">Events</h1>
          <button className="text-blue-600 underline font-semibold" onClick={onBack}>Back to Users</button>
        </div>
        {loading ? (
          <div className="text-gray-600 text-center">Loading events...</div>
        ) : error ? (
          <div className="text-red-500 mb-2" role="alert">{error}</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full border border-gray-200 rounded">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-2 text-left text-gray-700 font-semibold">Title</th>
                  <th className="px-4 py-2 text-left text-gray-700 font-semibold">Start</th>
                  <th className="px-4 py-2 text-left text-gray-700 font-semibold">End</th>
                </tr>
              </thead>
              <tbody>
                {events.map(e => (
                  <tr key={e.id} className="even:bg-gray-50 hover:bg-blue-50 transition">
                    <td className="px-4 py-2 font-medium text-gray-900">{e.title}</td>
                    <td className="px-4 py-2 text-gray-700">{new Date(e.startTime).toLocaleString()}</td>
                    <td className="px-4 py-2 text-gray-700">{new Date(e.endTime).toLocaleString()}</td>
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