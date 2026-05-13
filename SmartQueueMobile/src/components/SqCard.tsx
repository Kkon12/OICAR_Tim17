import React from 'react';
import { StyleSheet, View, ViewProps } from 'react-native';
import { colors } from '../theme/colors';
export function SqCard({ style, ...props }: ViewProps) { return <View {...props} style={[styles.card, style]} />; }
const styles = StyleSheet.create({ card: { backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: 12, padding: 18 } });
