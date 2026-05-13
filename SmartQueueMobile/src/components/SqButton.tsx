import React from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text } from 'react-native';
import { colors } from '../theme/colors';

export function SqButton({ title, onPress, variant = 'primary', loading = false, disabled = false }: { title: string; onPress: () => void; variant?: 'primary' | 'ghost' | 'danger'; loading?: boolean; disabled?: boolean }) {
  return <Pressable disabled={disabled || loading} onPress={onPress} style={({ pressed }) => [styles.base, styles[variant], (pressed || disabled || loading) && { opacity: 0.72 }]}>{loading ? <ActivityIndicator color={colors.white} /> : <Text style={[styles.text, variant === 'ghost' && styles.ghostText]}>{title}</Text>}</Pressable>;
}
const styles = StyleSheet.create({
  base: { minHeight: 46, borderRadius: 8, paddingVertical: 12, paddingHorizontal: 18, alignItems: 'center', justifyContent: 'center' },
  primary: { backgroundColor: colors.primary }, danger: { backgroundColor: colors.danger }, ghost: { backgroundColor: 'transparent', borderWidth: 1, borderColor: colors.border },
  text: { color: colors.white, fontWeight: '700', fontSize: 15 }, ghostText: { color: colors.muted }
});
