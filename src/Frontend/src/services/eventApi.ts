import axios from 'axios';
import type { CreateEventInput, EventDto, EventSummaryDto, PagedResult } from '../types';

// Token fijo para demo (en producción vendría de un flujo OAuth2/OIDC)
const DEMO_JWT = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
  'eyJzdWIiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDEiLCJuYW1lIjoiQWRtaW4gVXNlciIsInJvbGUiOiJhZG1pbiIsImlzcyI6ImV2ZW50LXBsYXRmb3JtIiwiYXVkIjoiZXZlbnQtcGxhdGZvcm0tY2xpZW50cyIsImV4cCI6OTk5OTk5OTk5OX0.' +
  'demo-signature';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor: adjunta JWT a cada request
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('jwt_token') ?? DEMO_JWT;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ─── Event API ────────────────────────────────────────────────────────────────

export const eventApi = {
  createEvent: async (data: CreateEventInput): Promise<EventDto> => {
    const res = await api.post<EventDto>('/api/events', data);
    return res.data;
  },

  getEvents: async (page = 1, pageSize = 20): Promise<PagedResult<EventSummaryDto>> => {
    const res = await api.get<PagedResult<EventSummaryDto>>('/api/events', {
      params: { page, pageSize },
    });
    return res.data;
  },

  getEventById: async (id: string): Promise<EventDto> => {
    const res = await api.get<EventDto>(`/api/events/${id}`);
    return res.data;
  },
};
