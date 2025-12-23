import React from 'react';
import { Header } from './components/layout/Header';
import { HomePage } from './pages/HomePage';
import './App.css';

const App: React.FC = () => {
  return (
    <div className="app">
      <Header />
      <main className="main-content">
        <HomePage />
      </main>
      <footer className="footer">
        <p>&copy; 2024 Adroit URL Shortener. Built with React + .NET 8.</p>
      </footer>
    </div>
  );
};

export default App;
