import React from 'react';
import { StyleSheet, Text, TextInput, TextInputProps, View } from 'react-native';
import { colors } from '../theme/colors';
export function SqInput({ label, left, ...props }: TextInputProps & { label: string; left?: string }) {
  return <View style={styles.wrap}><Text style={styles.label}>{label}</Text><View style={styles.box}>{left && <Text style={styles.left}>{left}</Text>}<TextInput placeholderTextColor={colors.muted} {...props} style={[styles.input, props.style]} /></View></View>;
}
const styles = StyleSheet.create({
  wrap: { gap: 7 }, label: { color: colors.muted, fontSize: 13, fontWeight: '600' },
  box: { flexDirection: 'row', alignItems: 'center', backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: 8 },
  left: { color: colors.muted, paddingLeft: 13, fontSize: 17 },
  input: { flex: 1, color: colors.text, paddingHorizontal: 12, paddingVertical: 12, fontSize: 16, outlineStyle: 'none' as never }
});
