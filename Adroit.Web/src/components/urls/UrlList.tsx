import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { fetchAllUrls } from '../../store/urlsSlice';
import { AppDispatch, RootState } from '../../store';
import { UrlCard } from './UrlCard';
import './UrlList.css';

export const UrlList: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { urls, isLoading, error } = useSelector((state: RootState) => state.urls);

  useEffect(() => {
    dispatch(fetchAllUrls());
  }, [dispatch]);

  if (isLoading && urls.length === 0) {
    return (
      <div className="url-list-container">
        <h2 className="list-title">Your Short URLs</h2>
        <div className="loading-state">
          <div className="loading-spinner"></div>
          <p>Loading URLs...</p>
        </div>
      </div>
    );
  }

  if (error && urls.length === 0) {
    return (
      <div className="url-list-container">
        <h2 className="list-title">Your Short URLs</h2>
        <div className="error-state">
          <p>Failed to load URLs: {error}</p>
          <button onClick={() => dispatch(fetchAllUrls())} className="retry-button">
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="url-list-container">
      <div className="list-header">
        <h2 className="list-title">Your Short URLs</h2>
        <span className="url-count">{urls.length} URLs</span>
      </div>

      {urls.length === 0 ? (
        <div className="empty-state">
          <span className="empty-icon">🔗</span>
          <h3>No URLs yet</h3>
          <p>Create your first short URL above!</p>
        </div>
      ) : (
        <div className="url-grid">
          {urls.map((url) => (
            <UrlCard key={url.id} url={url} />
          ))}
        </div>
      )}
    </div>
  );
};
