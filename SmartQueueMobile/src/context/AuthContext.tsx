import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { API_BASE_URL } from '../api/config';
import { User } from '../types/models';

type AuthContextValue = {
  user: User | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (firstName: string, lastName: string, email: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

// ── Extract user ID from JWT token
// The API doesn't return an id field directly, so we decode it from the token
function parseJwtId(token: string): string {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = JSON.parse(atob(base64));
    return (
      json.sub ??
      json.nameid ??
      json['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
      ''
    );
  } catch {
    return '';
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);

  // ── Restore session on app start
  useEffect(() => {
    (async () => {
      const token = await AsyncStorage.getItem('accessToken');
      const stored = await AsyncStorage.getItem('user');
      if (token && stored) {
        setAccessToken(token);
        setUser(JSON.parse(stored));
      }
    })();
  }, []);

  async function callAuth(path: string, body: object) {
    const res = await fetch(`${API_BASE_URL}/auth/${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || `Auth error ${res.status}`);
    }
    return res.json();
  }

  const value = useMemo<AuthContextValue>(() => ({
    user,
    accessToken,
    isAuthenticated: !!user,

    async login(email, password) {
      const data = await callAuth('login', { email, password });
      console.log('LOGIN RESPONSE:', JSON.stringify(data));

      const token = data.Token ?? data.accessToken ?? data.token ?? data.access_token;
      const refresh = data.RefreshToken ?? data.refreshToken ?? data.refresh_token ?? data.refreshtoken;

      if (!token) throw new Error('No access token in response');

      // ── Get user ID from JWT since API doesn't return it directly
      const userId = parseJwtId(token);
      console.log('PARSED USER ID:', userId);

      const userData: User = {
        id: userId,
        firstName: data.firstName ?? data.user?.firstName ?? '',
        lastName: data.lastName ?? data.user?.lastName ?? '',
        email: data.email ?? data.user?.email ?? email,
        role: data.role ?? data.user?.role ?? 'Korisnik',
      };

      await AsyncStorage.setItem('accessToken', token);
      if (refresh) await AsyncStorage.setItem('refreshToken', refresh);
      await AsyncStorage.setItem('user', JSON.stringify(userData));
      setAccessToken(token);
      setUser(userData);
    },

    async register(firstName, lastName, email, password) {
      await callAuth('register', { firstName, lastName, email, password });
      const data = await callAuth('login', { email, password });
      console.log('REGISTER/LOGIN RESPONSE:', JSON.stringify(data));

      const token = data.Token ?? data.accessToken ?? data.token ?? data.access_token;
      const refresh = data.RefreshToken ?? data.refreshToken ?? data.refresh_token ?? data.refreshtoken;

      if (!token) throw new Error('No access token in response');

      const userId = parseJwtId(token);
      console.log('PARSED USER ID:', userId);

      const userData: User = {
        id: userId,
        firstName: data.firstName ?? data.user?.firstName ?? firstName,
        lastName: data.lastName ?? data.user?.lastName ?? lastName,
        email: data.email ?? data.user?.email ?? email,
        role: data.role ?? data.user?.role ?? 'Korisnik',
      };

      await AsyncStorage.setItem('accessToken', token);
      if (refresh) await AsyncStorage.setItem('refreshToken', refresh);
      await AsyncStorage.setItem('user', JSON.stringify(userData));
      setAccessToken(token);
      setUser(userData);
    },

    logout() {
      AsyncStorage.removeItem('accessToken');
      AsyncStorage.removeItem('refreshToken');
      AsyncStorage.removeItem('user');
      setAccessToken(null);
      setUser(null);
    },

  }), [user, accessToken]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth mora biti unutar AuthProvider.');
  return ctx;
}