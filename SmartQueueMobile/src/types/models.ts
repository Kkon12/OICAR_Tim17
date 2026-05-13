export type User = { id: string; firstName: string; lastName: string; email: string; role: 'Korisnik' | 'Admin' | 'Djelatnik' };
export type QueueItem = { id: number; name: string; description: string; isActive: boolean; waitingCount: number; averageWaitMinutes: number; currentNumber: number; openCounters: number };
export type TicketStatus = 'Waiting' | 'Called' | 'InService' | 'Done' | 'Skipped';
export type Ticket = { id: number; queueId: number; queueName: string; number: number; status: TicketStatus; position: number; estimatedWaitMinutes: number; counterName?: string; createdAt: string };
