import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors } from '../theme/colors';
export function Brand({ large = false }: { large?: boolean }) {
  return <View style={styles.wrap}><Text style={[styles.icon, large && styles.iconLarge]}>▦</Text><Text style={[styles.text, large && styles.textLarge]}>SmartQueue</Text></View>;
}
const styles = StyleSheet.create({
  wrap: { alignItems: 'center', gap: 5 }, icon: { color: colors.primary, fontSize: 32, fontWeight: '900' }, iconLarge: { fontSize: 48 }, text: { color: colors.text, fontSize: 22, fontWeight: '800' }, textLarge: { fontSize: 34 }
});
