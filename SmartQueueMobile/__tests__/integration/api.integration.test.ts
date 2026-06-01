import AsyncStorage from '@react-native-async-storage/async-storage';
import { api } from '../../src/api/api';

const fetchMock = jest.fn();

global.fetch = fetchMock as unknown as typeof fetch;

function jsonResponse(body: unknown, ok = true, status = 200) {
  return Promise.resolve({
    ok,
    status,
    json: () => Promise.resolve(body),
    text: () => Promise.resolve(JSON.stringify(body))
  });
}

describe('api integration', () => {
  beforeEach(async () => {
    fetchMock.mockReset();
    await AsyncStorage.clear();
  });

  test('getQueues dohvaća redove i mapira API polja u mobilni model', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse([
      { id: 7, name: 'Referada', description: null, status: 'Active', totalWaiting: 4, defaultServiceMinutes: 6, openCounters: 2 },
      { id: 8, name: 'Financije', description: 'Plaćanja', status: 'Closed', totalWaiting: 0, defaultServiceMinutes: 5, openCounters: 0 }
    ]));

    const queues = await api.getQueues();

    expect(fetchMock).toHaveBeenCalledWith('http://localhost:5179/api/queue', expect.objectContaining({ headers: expect.objectContaining({ 'Content-Type': 'application/json' }) }));
    expect(queues).toEqual([
      { id: 7, name: 'Referada', description: '', isActive: true, waitingCount: 4, averageWaitMinutes: 6, currentNumber: 0, openCounters: 2 },
      { id: 8, name: 'Financije', description: 'Plaćanja', isActive: false, waitingCount: 0, averageWaitMinutes: 5, currentNumber: 0, openCounters: 0 }
    ]);
  });

  test('takeTicket šalje autorizirani POST i mapira kartu', async () => {
    await AsyncStorage.setItem('accessToken', 'token-123');
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 44, queueId: 3, queueName: 'Financije', ticketNumber: 55, status: 'Waiting', position: 2, estimatedWaitMinutes: 10, counterName: null, createdAt: '2026-06-01T10:00:00' }));

    const ticket = await api.takeTicket(3, 'user-9');

    expect(fetchMock).toHaveBeenCalledWith('http://localhost:5179/api/ticket/take', expect.objectContaining({
      method: 'POST',
      headers: expect.objectContaining({ Authorization: 'Bearer token-123' }),
      body: JSON.stringify({ queueId: 3, userId: 'user-9' })
    }));
    expect(ticket).toEqual({ id: 44, queueId: 3, queueName: 'Financije', number: 55, status: 'Waiting', position: 2, estimatedWaitMinutes: 10, counterName: null, createdAt: '2026-06-01T10:00:00' });
  });

  test('getMyTicket bira aktivnu kartu ispred završene', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse([
      { id: 1, queueId: 1, queueName: 'Opće informacije', ticketNumber: 100, status: 'Done', position: 0, estimatedWaitMinutes: 0, createdAt: 'staro' },
      { id: 2, queueId: 1, queueName: 'Opće informacije', ticketNumber: 101, status: 'Called', position: 0, estimatedWaitMinutes: 0, counterName: 'Šalter 1', createdAt: 'novo' }
    ]));

    const ticket = await api.getMyTicket();

    expect(ticket).toEqual({ id: 2, queueId: 1, queueName: 'Opće informacije', number: 101, status: 'Called', position: 0, estimatedWaitMinutes: 0, counterName: 'Šalter 1', createdAt: 'novo' });
  });

  test('getHistory vraća praznu listu kad API nema povijest', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse([]));

    const history = await api.getHistory();

    expect(fetchMock).toHaveBeenCalledWith('http://localhost:5179/api/ticket/history', expect.any(Object));
    expect(history).toEqual([]);
  });

  test('neuspješan API odgovor baca poruku iz servera', async () => {
    fetchMock.mockResolvedValueOnce(Promise.resolve({
      ok: false,
      status: 400,
      json: () => Promise.resolve({ message: 'Red nije aktivan.' }),
      text: () => Promise.resolve('')
    }));

    await expect(api.getQueues()).rejects.toThrow('Red nije aktivan.');
  });
});
