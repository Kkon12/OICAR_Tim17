const store = new Map<string, string>();

const AsyncStorage = {
  getItem: jest.fn((key: string) => Promise.resolve(store.get(key) ?? null)),
  setItem: jest.fn((key: string, value: string) => {
    store.set(key, value);
    return Promise.resolve();
  }),
  removeItem: jest.fn((key: string) => {
    store.delete(key);
    return Promise.resolve();
  }),
  clear: jest.fn(() => {
    store.clear();
    return Promise.resolve();
  }),
  __store: store
};

export default AsyncStorage;
