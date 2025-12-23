import { configureStore } from '@reduxjs/toolkit';
import urlsReducer from './urlsSlice';

export const store = configureStore({
  reducer: {
    urls: urlsReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
