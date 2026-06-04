export interface ZoneInput {
  name: string;
  price: number;
  capacity: number;
}

export interface CreateEventInput {
  name: string;
  date: string;
  location: string;
  zones: ZoneInput[];
}

export interface ZoneDto {
  id: string;
  name: string;
  price: number;
  capacity: number;
  availableCapacity: number;
}

export interface EventDto {
  id: string;
  name: string;
  date: string;
  location: string;
  status: string;
  organizerId: string;
  createdAt: string;
  zones: ZoneDto[];
}

export interface EventSummaryDto {
  id: string;
  name: string;
  date: string;
  location: string;
  status: string;
  totalCapacity: number;
  availableCapacity: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiError {
  error?: string;
  errors?: Record<string, string[]>;
}
