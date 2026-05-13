import React, { createContext, useContext, useMemo, useState } from 'react';
import { api } from '../api/api';
import { User } from '../types/models';

type AuthContextValue = { user: User | null; isAuthenticated: boolean; login: (email: string, password: string) => Promise<void>; register: (firstName: string, lastName: string, email: string, password: string) => Promise<void>; logout: () => void };
const AuthContext = createContext<AuthContextValue | undefined>(undefined);
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const value = useMemo<AuthContextValue>(() => ({ user, isAuthenticated: !!user, async login(email, password) { setUser((await api.login(email, password)).user); }, async register(firstName, lastName, email, password) { setUser((await api.register(firstName, lastName, email, password)).user); }, logout() { setUser(null); } }), [user]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
export function useAuth() { const ctx = useContext(AuthContext); if (!ctx) throw new Error('useAuth mora biti unutar AuthProvider.'); return ctx; }
