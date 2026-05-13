import { mockHistory, mockQueues } from '../data/mockData';
import { QueueItem, Ticket, User } from '../types/models';

let activeTicket: Ticket | null = null;
let nextTicketId = 10000;
const delay = <T,>(value: T, ms = 250) => new Promise<T>((resolve) => setTimeout(() => resolve(value), ms));

export const mockApi = {
  async login(email: string, password: string): Promise<{ user: User; accessToken: string; refreshToken: string }> {
    if (!email || !password) throw new Error('Unesi email i lozinku.');
    return delay({ user: { id: 'mock-user-1', firstName: 'Ante', lastName: 'Korisnik', email, role: 'Korisnik' }, accessToken: 'mock-access-token', refreshToken: 'mock-refresh-token' });
  },
  async register(firstName: string, lastName: string, email: string, password: string) {
    if (!firstName || !lastName || !email || password.length < 6) throw new Error('Provjeri podatke. Lozinka mora imati barem 6 znakova.');
    return this.login(email, password);
  },
  async getQueues(): Promise<QueueItem[]> { return delay(mockQueues); },
  async takeTicket(queueId: number): Promise<Ticket> {
    const queue = mockQueues.find((q) => q.id === queueId);
    if (!queue || !queue.isActive) throw new Error('Red nije aktivan.');
    activeTicket = {
      id: nextTicketId++, queueId: queue.id, queueName: queue.name,
      number: queue.currentNumber + queue.waitingCount + 1,
      status: 'Waiting', position: queue.waitingCount + 1,
      estimatedWaitMinutes: (queue.waitingCount + 1) * queue.averageWaitMinutes,
      createdAt: new Date().toLocaleString('hr-HR')
    };
    return delay(activeTicket);
  },
  async getMyTicket(): Promise<Ticket | null> { return delay(activeTicket); },
  async getHistory(): Promise<Ticket[]> { return delay(activeTicket ? [activeTicket, ...mockHistory] : mockHistory); },
  async simulateProgress(): Promise<Ticket | null> {
    if (!activeTicket) return delay(null);
    if (activeTicket.status === 'Waiting' && activeTicket.position > 1) activeTicket = { ...activeTicket, position: activeTicket.position - 1, estimatedWaitMinutes: Math.max(1, activeTicket.estimatedWaitMinutes - 4) };
    else if (activeTicket.status === 'Waiting') activeTicket = { ...activeTicket, status: 'Called', position: 0, estimatedWaitMinutes: 0, counterName: 'Šalter 2' };
    else if (activeTicket.status === 'Called') activeTicket = { ...activeTicket, status: 'InService' };
    else if (activeTicket.status === 'InService') activeTicket = { ...activeTicket, status: 'Done' };
    return delay(activeTicket, 150);
  }
};
