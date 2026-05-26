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

type Props = NativeStackScreenProps<RootStackParamList, 'Login'>;
export function LoginScreen({ navigation }: Props) {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function submit() {
    setError('');
    setLoading(true);
    try {
      await login(email, password);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Greška kod prijave.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <Screen style={styles.page}>
      <View style={styles.card}>
        <Brand large />
        <Text style={styles.subtitle}>Sign in to your account</Text>
        <SqInput label="Email" left="✉" value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" placeholder="you@smartqueue.com" />
        <SqInput label="Password" left="⌕" value={password} onChangeText={setPassword} secureTextEntry placeholder="••••••••" />
        {!!error && <Text style={styles.error}>⚠ {error}</Text>}
        <SqButton title="↪ Sign In" onPress={submit} loading={loading} />
        <SqButton title="Create customer account" variant="ghost" onPress={() => navigation.navigate('Register')} />
      </View>
      <Text style={styles.footer}>SmartQueue © 2026 — Digital Queue Management</Text>
    </Screen>
  );
}

const styles = StyleSheet.create({
  page: { justifyContent: 'center', maxWidth: 470, width: '100%', alignSelf: 'center' },
  card: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: 24, gap: 16 },
  subtitle: { color: colors.muted, textAlign: 'center', marginBottom: 4 },
  error: { color: colors.danger, fontWeight: '700' },
  footer: { color: colors.muted, fontSize: 12, textAlign: 'center', marginTop: 14 }
});