import { API_BASE_URL, API_MODE } from './config';
import { mockApi } from './mockApi';
import { QueueItem, Ticket } from '../types/models';

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers: { 'Content-Type': 'application/json', ...(options.headers || {}) } });
  if (!response.ok) throw new Error(await response.text() || `API greška ${response.status}`);
  return response.json() as Promise<T>;
}

export const api = {
  login: mockApi.login,
  register: mockApi.register,
  getQueues(): Promise<QueueItem[]> { return API_MODE === 'mock' ? mockApi.getQueues() : request<QueueItem[]>('/queue'); },
  takeTicket(queueId: number): Promise<Ticket> { return API_MODE === 'mock' ? mockApi.takeTicket(queueId) : request<Ticket>('/ticket/take', { method: 'POST', body: JSON.stringify({ queueId }) }); },
  getMyTicket(): Promise<Ticket | null> { return API_MODE === 'mock' ? mockApi.getMyTicket() : request<Ticket | null>('/ticket/my'); },
  getHistory(): Promise<Ticket[]> { return API_MODE === 'mock' ? mockApi.getHistory() : request<Ticket[]>('/ticket/history'); },
  simulateProgress: mockApi.simulateProgress
};
