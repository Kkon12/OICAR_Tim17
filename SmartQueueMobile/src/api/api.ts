import { API_BASE_URL, API_MODE } from './config';
import { mockApi } from './mockApi';
import { QueueItem, Ticket } from '../types/models';

// ── Gets JWT token from storage
async function getToken(): Promise<string | null> {
  try {
    const AsyncStorage = (await import('@react-native-async-storage/async-storage')).default;
    return await AsyncStorage.getItem('accessToken');
  } catch {
    return null;
  }
}

// ── Base request function — attaches JWT automatically
async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = await getToken();

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers || {}),
    },
  });

  if (!response.ok) {
    const err = await response.json().catch(() => ({}));
    throw new Error(err.message || `API greška ${response.status}`);
  }

  const text = await response.text();
  return text ? JSON.parse(text) : (null as unknown as T);
}

function mapTicket(t: any): Ticket {
  return {
    id: t.id,
    queueId: t.queueId,
    queueName: t.queueName,
    number: t.ticketNumber,
    status: t.status,
    position: t.position,
    estimatedWaitMinutes: t.estimatedWaitMinutes,
    counterName: t.counterName,
    createdAt: t.createdAt,
  };
}

export const api = {
  login: mockApi.login,
  register: mockApi.register,

  // ── GET /api/queue
  getQueues(): Promise<QueueItem[]> {
    if (API_MODE === 'mock') return mockApi.getQueues();
    return request<any[]>('/queue').then(queues =>
      queues.map(q => ({
        id: q.id,
        name: q.name,
        description: q.description ?? '',
        isActive: q.status === 'Active',
        waitingCount: q.totalWaiting,
        averageWaitMinutes: q.defaultServiceMinutes,
        currentNumber: 0,
        openCounters: q.openCounters,
      }))
    );
  },

  // ── POST /api/ticket/take
  // Shows alert with ticket number and navigates to Ticket tab
  takeTicket(queueId: number, userId?: string): Promise<Ticket> {
    if (API_MODE === 'mock') return mockApi.takeTicket(queueId);
    return request<any>('/ticket/take', {
      method: 'POST',
      body: JSON.stringify({ queueId, userId }),
    }).then(t => {
      console.log('TAKE TICKET RESPONSE:', JSON.stringify(t));
      return mapTicket(t);
    });
  },

  // ── GET /api/ticket/my
  // Returns array — we take the most recent Waiting or Called ticket
  getMyTicket(): Promise<Ticket | null> {
    if (API_MODE === 'mock') return mockApi.getMyTicket();
    return request<any[]>('/ticket/my').then(tickets => {
      console.log('MY TICKETS RESPONSE:', JSON.stringify(tickets));
      if (!tickets || tickets.length === 0) return null;
      // Find active ticket — prefer Waiting or Called status
      const active = tickets.find(t =>
        t.status === 'Waiting' || t.status === 'Called' || t.status === 'InService'
      ) ?? tickets[0];
      return mapTicket(active);
    });
  },

  // ── GET /api/ticket/history
  getHistory(): Promise<Ticket[]> {
    if (API_MODE === 'mock') return mockApi.getHistory();
    return request<any[]>('/ticket/history').then(tickets => {
      if (!tickets || tickets.length === 0) return [];
      return tickets.map(mapTicket);
    });
  },

  simulateProgress: mockApi.simulateProgress,
};