import React from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../../store';
import { selectUrl, clearSelectedStats } from '../../store/urlsSlice';
import { useClipboard } from '../../hooks/useClipboard';
import './UrlStats.css';

export const UrlStats: React.FC = () => {
  const dispatch = useDispatch();
  const { selectedUrl, selectedStats, isLoading } = useSelector(
    (state: RootState) => state.urls
  );
  const { copied, copy } = useClipboard();

  if (!selectedUrl) return null;

  const handleClose = () => {
    dispatch(selectUrl(null));
    dispatch(clearSelectedStats());
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <div className="stats-overlay" onClick={handleClose}>
      <div className="stats-modal" onClick={(e) => e.stopPropagation()}>
        <div className="stats-header">
          <h2>URL Statistics</h2>
          <button className="close-button" onClick={handleClose}>
            ✕
          </button>
        </div>

        {isLoading ? (
          <div className="stats-loading">
            <div className="loading-spinner"></div>
            <p>Loading statistics...</p>
          </div>
        ) : selectedStats ? (
          <div className="stats-content">
            <div className="stats-url-info">
              <div className="stat-group">
                <label>Short URL</label>
                <div className="url-with-copy">
                  <a
                    href={selectedStats.shortUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="stats-short-url"
                  >
                    {selectedStats.shortUrl}
                  </a>
                  <button
                    className="copy-btn"
                    onClick={() => copy(selectedStats.shortUrl)}
                  >
                    {copied ? '✓' : '📋'}
                  </button>
                </div>
              </div>

              <div className="stat-group">
                <label>Original URL</label>
                <a
                  href={selectedStats.longUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="stats-long-url"
                >
                  {selectedStats.longUrl}
                </a>
              </div>
            </div>

            <div className="stats-metrics">
              <div className="metric-card primary">
                <span className="metric-value">{selectedStats.clickCount}</span>
                <span className="metric-label">Total Clicks</span>
              </div>

              <div className="metric-card">
                <span className="metric-value">
                  {selectedStats.averageClicksPerDay.toFixed(1)}
                </span>
                <span className="metric-label">Avg. Clicks/Day</span>
              </div>

              <div className="metric-card">
                <span className="metric-value">{selectedStats.daysSinceCreation}</span>
                <span className="metric-label">Days Active</span>
              </div>
            </div>

            <div className="stats-dates">
              <div className="date-item">
                <span className="date-label">Created</span>
                <span className="date-value">{formatDate(selectedStats.createdAt)}</span>
              </div>
              {selectedStats.lastAccessedAt && (
                <div className="date-item">
                  <span className="date-label">Last Accessed</span>
                  <span className="date-value">
                    {formatDate(selectedStats.lastAccessedAt)}
                  </span>
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="stats-error">
            <p>Failed to load statistics</p>
          </div>
        )}
      </div>
    </div>
  );
};
