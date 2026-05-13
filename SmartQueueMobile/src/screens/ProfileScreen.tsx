import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Screen } from '../components/Screen';
import { SqCard } from '../components/SqCard';
import { SqButton } from '../components/SqButton';
import { PageHeader } from '../components/PageHeader';
import { colors } from '../theme/colors';
import { useAuth } from '../context/AuthContext';

export function ProfileScreen() {
  const { user, logout } = useAuth();
  return <Screen><PageHeader title="Profil" /><SqCard style={styles.card}><View style={styles.avatar}><Text style={styles.avatarText}>{user?.firstName?.[0] ?? '?'}</Text></View><Text style={styles.name}>{user?.firstName} {user?.lastName}</Text><Text style={styles.email}>{user?.email}</Text><View style={styles.role}><Text style={styles.roleText}>{user?.role ?? 'Korisnik'}</Text></View></SqCard><SqCard><Text style={styles.sectionTitle}>Backend povezivanje kasnije</Text></SqCard><SqButton title="Odjava" variant="danger" onPress={logout} /></Screen>;
}
const styles = StyleSheet.create({ card: { alignItems: 'center' }, avatar: { width: 66, height: 66, borderRadius: 33, backgroundColor: colors.primary, alignItems: 'center', justifyContent: 'center', marginBottom: 12 }, avatarText: { color: colors.white, fontSize: 30, fontWeight: '900' }, name: { color: colors.text, fontSize: 22, fontWeight: '900' }, email: { color: colors.muted, marginTop: 4 }, role: { backgroundColor: 'rgba(79,142,247,0.12)', borderRadius: 99, paddingHorizontal: 13, paddingVertical: 6, marginTop: 13 }, roleText: { color: colors.primary, fontWeight: '900' }, sectionTitle: { color: colors.text, fontWeight: '900', marginBottom: 6 }, note: { color: colors.muted, lineHeight: 20 } });
