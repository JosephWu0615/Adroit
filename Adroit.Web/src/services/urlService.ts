import { apiClient } from './api';
import { ShortUrl, UrlStats, CreateUrlRequest, ApiResponse } from '../types/url';

export const urlService = {
  async createShortUrl(request: CreateUrlRequest): Promise<ShortUrl> {
    const { data } = await apiClient.post<ApiResponse<ShortUrl>>('/urls', request);
    if (!data.success) {
      throw new Error(data.error || 'Failed to create short URL');
    }
    return data.data!;
  },

  async getAllUrls(): Promise<ShortUrl[]> {
    const { data } = await apiClient.get<ApiResponse<ShortUrl[]>>('/urls');
    if (!data.success) {
      throw new Error(data.error || 'Failed to fetch URLs');
    }
    return data.data || [];
  },

  async getUrlDetails(shortCode: string): Promise<ShortUrl> {
    const { data } = await apiClient.get<ApiResponse<ShortUrl>>(`/urls/${shortCode}`);
    if (!data.success) {
      throw new Error(data.error || 'URL not found');
    }
    return data.data!;
  },

  async deleteShortUrl(shortCode: string): Promise<void> {
    await apiClient.delete(`/urls/${shortCode}`);
  },

  async getUrlStats(shortCode: string): Promise<UrlStats> {
    const { data } = await apiClient.get<ApiResponse<UrlStats>>(`/urls/${shortCode}/stats`);
    if (!data.success) {
      throw new Error(data.error || 'Failed to fetch stats');
    }
    return data.data!;
  },

  async lookupByLongUrl(longUrl: string): Promise<ShortUrl[]> {
    const { data } = await apiClient.get<ApiResponse<ShortUrl[]>>('/urls/lookup', {
      params: { longUrl },
    });
    if (!data.success) {
      throw new Error(data.error || 'Lookup failed');
    }
    return data.data || [];
  },
};
