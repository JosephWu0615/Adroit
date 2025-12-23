import React from 'react';
import './Header.css';

export const Header: React.FC = () => {
  return (
    <header className="header">
      <div className="header-container">
        <div className="header-logo">
          <span className="logo-icon">🔗</span>
          <h1 className="logo-text">Adroit</h1>
        </div>
        <p className="header-tagline">URL Shortener</p>
      </div>
    </header>
  );
};
