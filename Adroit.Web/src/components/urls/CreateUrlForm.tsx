import React, { useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createUrl, clearError, clearSuccess } from '../../store/urlsSlice';
import { AppDispatch, RootState } from '../../store';
import './CreateUrlForm.css';

export const CreateUrlForm: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { isLoading, error, successMessage } = useSelector((state: RootState) => state.urls);

  const [longUrl, setLongUrl] = useState('');
  const [customCode, setCustomCode] = useState('');
  const [showCustomCode, setShowCustomCode] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!longUrl.trim()) return;

    dispatch(clearError());
    dispatch(clearSuccess());

    try {
      await dispatch(createUrl({
        longUrl: longUrl.trim(),
        customShortCode: customCode.trim() || undefined,
      })).unwrap();

      setLongUrl('');
      setCustomCode('');
      setShowCustomCode(false);
    } catch (err) {
      // Error is handled by Redux
    }
  };

  return (
    <div className="create-url-form-container">
      <h2 className="form-title">Shorten a URL</h2>

      <form onSubmit={handleSubmit} className="create-url-form">
        <div className="form-group">
          <label htmlFor="longUrl" className="form-label">
            Enter your long URL
          </label>
          <input
            id="longUrl"
            type="url"
            value={longUrl}
            onChange={(e) => setLongUrl(e.target.value)}
            placeholder="https://example.com/very/long/url/that/needs/shortening"
            className="form-input"
            required
          />
        </div>

        <div className="custom-code-toggle">
          <button
            type="button"
            className="toggle-button"
            onClick={() => setShowCustomCode(!showCustomCode)}
          >
            {showCustomCode ? '− Hide custom code' : '+ Add custom code'}
          </button>
        </div>

        {showCustomCode && (
          <div className="form-group">
            <label htmlFor="customCode" className="form-label">
              Custom short code (optional)
            </label>
            <input
              id="customCode"
              type="text"
              value={customCode}
              onChange={(e) => setCustomCode(e.target.value)}
              placeholder="my-custom-code"
              className="form-input"
              pattern="[a-zA-Z0-9]{4,12}"
              title="4-12 alphanumeric characters"
            />
            <p className="form-hint">4-12 alphanumeric characters only</p>
          </div>
        )}

        {error && (
          <div className="message error-message">
            {error}
          </div>
        )}

        {successMessage && (
          <div className="message success-message">
            {successMessage}
          </div>
        )}

        <button
          type="submit"
          className="submit-button"
          disabled={isLoading || !longUrl.trim()}
        >
          {isLoading ? 'Creating...' : 'Shorten URL'}
        </button>
      </form>
    </div>
  );
};
