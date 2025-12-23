import React from 'react';
import { useDispatch } from 'react-redux';
import { ShortUrl } from '../../types/url';
import { deleteUrl, fetchUrlStats, selectUrl } from '../../store/urlsSlice';
import { AppDispatch } from '../../store';
import { useClipboard } from '../../hooks/useClipboard';
import './UrlCard.css';

interface UrlCardProps {
  url: ShortUrl;
}

export const UrlCard: React.FC<UrlCardProps> = ({ url }) => {
  const dispatch = useDispatch<AppDispatch>();
  const { copied, copy } = useClipboard();

  const handleCopy = () => {
    copy(url.shortUrl);
  };

  const handleDelete = () => {
    if (window.confirm(`Are you sure you want to delete "${url.shortCode}"?`)) {
      dispatch(deleteUrl(url.shortCode));
    }
  };

  const handleViewStats = () => {
    dispatch(selectUrl(url));
    dispatch(fetchUrlStats(url.shortCode));
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const truncateUrl = (urlString: string, maxLength: number = 50) => {
    if (urlString.length <= maxLength) return urlString;
    return urlString.substring(0, maxLength) + '...';
  };

  return (
    <div className="url-card">
      <div className="url-card-main">
        <div className="url-info">
          <a
            href={url.shortUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="short-url"
          >
            {url.shortUrl}
          </a>
          <p className="long-url" title={url.longUrl}>
            {truncateUrl(url.longUrl)}
          </p>
        </div>

        <div className="url-stats">
          <span className="click-count">
            <span className="click-icon">👆</span>
            {url.clickCount} clicks
          </span>
          <span className="created-date">
            Created {formatDate(url.createdAt)}
          </span>
        </div>
      </div>

      <div className="url-card-actions">
        <button
          className="action-button copy-button"
          onClick={handleCopy}
          title="Copy short URL"
        >
          {copied ? '✓ Copied!' : '📋 Copy'}
        </button>
        <button
          className="action-button stats-button"
          onClick={handleViewStats}
          title="View statistics"
        >
          📊 Stats
        </button>
        <button
          className="action-button delete-button"
          onClick={handleDelete}
          title="Delete URL"
        >
          🗑️ Delete
        </button>
      </div>
    </div>
  );
};
