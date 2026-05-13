import React from 'react';
import { SafeAreaView, ScrollView, StyleSheet, View, ViewStyle } from 'react-native';
import { colors } from '../theme/colors';

export function Screen({ children, scroll = true, style }: { children: React.ReactNode; scroll?: boolean; style?: ViewStyle }) {
  const body = <View style={[styles.content, style]}>{children}</View>;
  return <SafeAreaView style={styles.root}>{scroll ? <ScrollView contentContainerStyle={styles.scroll}>{body}</ScrollView> : body}</SafeAreaView>;
}
const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg },
  scroll: { flexGrow: 1 },
  content: { flexGrow: 1, padding: 20, gap: 16, backgroundColor: colors.bg }
});
