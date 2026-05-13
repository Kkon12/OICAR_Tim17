# SmartQueue Mobile

Expo React Native mobile frontend. Trenutno radi u mock modu bez Docker/PostgreSQL/backend servera.

## Pokretanje

```bash
npm install
npm start
```

Za web pritisni `w` u Expo terminalu ili pokreni:

```bash
npm run web
```

## Mock podaci

- `src/data/mockData.ts`
- `src/api/mockApi.ts`

## Kasnije spajanje na backend

U `src/api/config.ts` promijeni:

```ts
export const API_MODE: 'mock' | 'real' = 'real';
export const API_BASE_URL = 'http://localhost:5000/api';
```

Za Android emulator koristi `http://10.0.2.2:5000/api`.
Za fizički mobitel koristi IP računala, npr. `http://192.168.0.197:5000/api`.
