export interface ShortUrl {
  id: string;
  shortCode: string;
  shortUrl: string;
  longUrl: string;
  clickCount: number;
  createdAt: string;
  lastAccessedAt?: string;
}

export interface UrlStats {
  shortCode: string;
  shortUrl: string;
  longUrl: string;
  clickCount: number;
  createdAt: string;
  lastAccessedAt?: string;
  averageClicksPerDay: number;
  daysSinceCreation: number;
}

export interface CreateUrlRequest {
  longUrl: string;
  customShortCode?: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}
