import React, { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Screen } from '../components/Screen';
import { SqButton } from '../components/SqButton';
import { SqInput } from '../components/SqInput';
import { Brand } from '../components/Brand';
import { colors } from '../theme/colors';
import { useAuth } from '../context/AuthContext';
import { RootStackParamList } from '../navigation/AppNavigator';

type Props = NativeStackScreenProps<RootStackParamList, 'Register'>;
export function RegisterScreen({ navigation }: Props) {
  const { register } = useAuth();
  const [firstName, setFirstName] = useState('Ante'); const [lastName, setLastName] = useState('Korisnik'); const [email, setEmail] = useState('ante@example.com'); const [password, setPassword] = useState('password'); const [error, setError] = useState(''); const [loading, setLoading] = useState(false);
  async function submit() { setError(''); setLoading(true); try { await register(firstName, lastName, email, password); } catch (e) { setError(e instanceof Error ? e.message : 'Greška kod registracije.'); } finally { setLoading(false); } }
  return <Screen style={styles.page}><View style={styles.card}><Brand /><Text style={styles.title}>Registracija</Text><Text style={styles.subtitle}>Korisnički račun za uzimanje i praćenje ticketa.</Text><SqInput label="Ime" value={firstName} onChangeText={setFirstName} /><SqInput label="Prezime" value={lastName} onChangeText={setLastName} /><SqInput label="Email" value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" /><SqInput label="Lozinka" value={password} onChangeText={setPassword} secureTextEntry />{!!error && <Text style={styles.error}>⚠ {error}</Text>}<SqButton title="Registriraj se" onPress={submit} loading={loading} /><SqButton title="Nazad na prijavu" variant="ghost" onPress={() => navigation.goBack()} /></View></Screen>;
}
const styles = StyleSheet.create({ page: { justifyContent: 'center', maxWidth: 520, width: '100%', alignSelf: 'center' }, card: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: 24, gap: 14 }, title: { color: colors.text, fontSize: 25, fontWeight: '800', textAlign: 'center' }, subtitle: { color: colors.muted, textAlign: 'center' }, error: { color: colors.danger, fontWeight: '700' } });
