import React, { useState } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, Link } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import UsersPage from './pages/UsersPage';
import EventsPage from './pages/EventsPage';

function NavBar({ onLogout }: { onLogout: () => void }) {
  return (
    <nav className="bg-gray-900 text-white px-4 py-3 flex items-center justify-between mb-8">
      <div className="flex gap-4">
        <Link to="/users" className="hover:text-blue-400 font-semibold">Users</Link>
        <Link to="/events" className="hover:text-blue-400 font-semibold">Events</Link>
      </div>
      <button onClick={onLogout} className="bg-red-600 hover:bg-red-700 px-3 py-1 rounded font-semibold">Logout</button>
    </nav>
  );
}

function AppRoutes({ token, onLogout, onAuth }: { token: string | null, onLogout: () => void, onAuth: (token: string) => void }) {
  const [page, setPage] = useState<'users' | 'events'>('users');
  if (!token) {
    return (
      <Routes>
        <Route path="/login" element={<LoginPage onAuth={onAuth} />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    );
  }
  return (
    <>
      <NavBar onLogout={onLogout} />
      <Routes>
        <Route path="/users" element={<UsersPage onLogout={onLogout} onGoToEvents={() => setPage('events')} />} />
        <Route path="/events" element={<EventsPage onBack={() => setPage('users')} />} />
        <Route path="*" element={<Navigate to={`/${page}`} replace />} />
      </Routes>
    </>
  );
}

function App() {
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));

  const handleAuth = (tok: string) => {
    setToken(tok);
  };
  const handleLogout = () => {
    setToken(null);
    localStorage.removeItem('token');
  };

  return (
    <Router>
      <AppRoutes token={token} onLogout={handleLogout} onAuth={handleAuth} />
    </Router>
  );
}

export default App;
