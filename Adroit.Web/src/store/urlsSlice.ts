import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { urlService } from '../services/urlService';
import { ShortUrl, CreateUrlRequest, UrlStats } from '../types/url';

interface UrlsState {
  urls: ShortUrl[];
  selectedUrl: ShortUrl | null;
  selectedStats: UrlStats | null;
  isLoading: boolean;
  error: string | null;
  successMessage: string | null;
}

const initialState: UrlsState = {
  urls: [],
  selectedUrl: null,
  selectedStats: null,
  isLoading: false,
  error: null,
  successMessage: null,
};

// Async thunks
export const fetchAllUrls = createAsyncThunk(
  'urls/fetchAll',
  async (_, { rejectWithValue }) => {
    try {
      return await urlService.getAllUrls();
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.error || error.message);
    }
  }
);

export const createUrl = createAsyncThunk(
  'urls/create',
  async (request: CreateUrlRequest, { rejectWithValue }) => {
    try {
      return await urlService.createShortUrl(request);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.error || error.message);
    }
  }
);

export const deleteUrl = createAsyncThunk(
  'urls/delete',
  async (shortCode: string, { rejectWithValue }) => {
    try {
      await urlService.deleteShortUrl(shortCode);
      return shortCode;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.error || error.message);
    }
  }
);

export const fetchUrlStats = createAsyncThunk(
  'urls/fetchStats',
  async (shortCode: string, { rejectWithValue }) => {
    try {
      return await urlService.getUrlStats(shortCode);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.error || error.message);
    }
  }
);

const urlsSlice = createSlice({
  name: 'urls',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    clearSuccess: (state) => {
      state.successMessage = null;
    },
    selectUrl: (state, action: PayloadAction<ShortUrl | null>) => {
      state.selectedUrl = action.payload;
      state.selectedStats = null;
    },
    clearSelectedStats: (state) => {
      state.selectedStats = null;
    },
  },
  extraReducers: (builder) => {
    // Fetch all URLs
    builder
      .addCase(fetchAllUrls.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchAllUrls.fulfilled, (state, action) => {
        state.isLoading = false;
        state.urls = action.payload;
      })
      .addCase(fetchAllUrls.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    // Create URL
    builder
      .addCase(createUrl.pending, (state) => {
        state.isLoading = true;
        state.error = null;
        state.successMessage = null;
      })
      .addCase(createUrl.fulfilled, (state, action) => {
        state.isLoading = false;
        state.urls.unshift(action.payload);
        state.successMessage = 'Short URL created successfully!';
      })
      .addCase(createUrl.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    // Delete URL
    builder
      .addCase(deleteUrl.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(deleteUrl.fulfilled, (state, action) => {
        state.isLoading = false;
        state.urls = state.urls.filter((u) => u.shortCode !== action.payload);
        state.successMessage = 'URL deleted successfully!';
        if (state.selectedUrl?.shortCode === action.payload) {
          state.selectedUrl = null;
          state.selectedStats = null;
        }
      })
      .addCase(deleteUrl.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    // Fetch URL stats
    builder
      .addCase(fetchUrlStats.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchUrlStats.fulfilled, (state, action) => {
        state.isLoading = false;
        state.selectedStats = action.payload;
      })
      .addCase(fetchUrlStats.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });
  },
});

export const { clearError, clearSuccess, selectUrl, clearSelectedStats } = urlsSlice.actions;
export default urlsSlice.reducer;
