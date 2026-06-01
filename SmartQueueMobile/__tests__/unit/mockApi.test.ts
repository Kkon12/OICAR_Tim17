async function loadMockApi() {
  jest.resetModules();
  return import('../../src/api/mockApi');
}

describe('mockApi unit', () => {
  test('login vraća korisnika i tokene za ispravne podatke', async () => {
    const { mockApi } = await loadMockApi();

    const result = await mockApi.login('ante@test.hr', 'tajna123');

    expect(result.user.email).toBe('ante@test.hr');
    expect(result.user.role).toBe('Korisnik');
    expect(result.accessToken).toBe('mock-access-token');
    expect(result.refreshToken).toBe('mock-refresh-token');
  });

  test('login odbija prazan email ili lozinku', async () => {
    const { mockApi } = await loadMockApi();

    await expect(mockApi.login('', 'tajna123')).rejects.toThrow('Unesi email i lozinku.');
    await expect(mockApi.login('ante@test.hr', '')).rejects.toThrow('Unesi email i lozinku.');
  });

  test('register odbija lozinku kraću od šest znakova', async () => {
    const { mockApi } = await loadMockApi();

    await expect(mockApi.register('Ante', 'Korisnik', 'ante@test.hr', '12345')).rejects.toThrow('Provjeri podatke. Lozinka mora imati barem 6 znakova.');
  });

  test('takeTicket kreira kartu prema stanju aktivnog reda', async () => {
    const { mockApi } = await loadMockApi();

    const ticket = await mockApi.takeTicket(2);

    expect(ticket.queueId).toBe(2);
    expect(ticket.queueName).toBe('Studentska služba');
    expect(ticket.number).toBe(230);
    expect(ticket.position).toBe(12);
    expect(ticket.estimatedWaitMinutes).toBe(84);
    expect(ticket.status).toBe('Waiting');
  });

  test('simulateProgress pomiče kartu kroz poziciju i statuse', async () => {
    const { mockApi } = await loadMockApi();
    await mockApi.takeTicket(1);

    const called = await mockApi.simulateProgress();
    const inService = await mockApi.simulateProgress();
    const done = await mockApi.simulateProgress();

    expect(called?.status).toBe('Called');
    expect(called?.counterName).toBe('Šalter 2');
    expect(inService?.status).toBe('InService');
    expect(done?.status).toBe('Done');
  });
});
