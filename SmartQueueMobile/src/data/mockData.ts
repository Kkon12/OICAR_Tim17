import { QueueItem, Ticket } from '../types/models';

export const mockQueues: QueueItem[] = [
  { id: 1, name: 'Opće informacije', description: 'Upiti, potvrde i osnovne usluge.', isActive: true, waitingCount: 0, averageWaitMinutes: 4, currentNumber: 104, openCounters: 2 },
  { id: 2, name: 'Studentska služba', description: 'Indeks, potvrde, prijave i status studenta.', isActive: true, waitingCount: 11, averageWaitMinutes: 7, currentNumber: 218, openCounters: 3 },
  { id: 3, name: 'Financije', description: 'Uplate, računi, školarine i troškovi.', isActive: true, waitingCount: 3, averageWaitMinutes: 5, currentNumber: 52, openCounters: 1 },
  { id: 4, name: 'Referada', description: 'Administrativni zahtjevi i dokumenti.', isActive: false, waitingCount: 0, averageWaitMinutes: 0, currentNumber: 87, openCounters: 0 }
];

export const mockHistory: Ticket[] = [
  { id: 9001, queueId: 2, queueName: 'Studentska služba', number: 214, status: 'Done', position: 0, estimatedWaitMinutes: 0, counterName: 'Šalter 2', createdAt: '11/03/2026 10:24:00' },
  { id: 9002, queueId: 1, queueName: 'Opće informacije', number: 101, status: 'Skipped', position: 0, estimatedWaitMinutes: 0, counterName: 'Šalter 1', createdAt: '10/03/2026 13:12:00' }
];
