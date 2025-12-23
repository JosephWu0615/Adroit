import React from 'react';
import { CreateUrlForm } from '../components/urls/CreateUrlForm';
import { UrlList } from '../components/urls/UrlList';
import { UrlStats } from '../components/urls/UrlStats';
import './HomePage.css';

export const HomePage: React.FC = () => {
  return (
    <div className="home-page">
      <CreateUrlForm />
      <UrlList />
      <UrlStats />
    </div>
  );
};
