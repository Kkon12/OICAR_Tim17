import React, { useCallback, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Screen } from '../components/Screen';
import { SqCard } from '../components/SqCard';
import { PageHeader } from '../components/PageHeader';
import { colors } from '../theme/colors';
import { api } from '../api/api';
import { Ticket } from '../types/models';

function statusColor(status: Ticket['status']) { if (status === 'Done') return colors.success; if (status === 'Skipped') return colors.danger; if (status === 'Waiting') return colors.warning; if (status === 'Called') return colors.primary; return colors.text; }
export function HistoryScreen() {
  const [items, setItems] = useState<Ticket[]>([]);
  useFocusEffect(useCallback(() => { api.getHistory().then(setItems); }, []));
  return <Screen><PageHeader title="Povijest" subtitle="Pregled prethodnih i aktivnih ticketa." />{items.map((t) => <SqCard key={t.id}><View style={styles.row}><Text style={styles.number}>#{t.number}</Text><Text style={[styles.status, { color: statusColor(t.status) }]}>{t.status}</Text></View><Text style={styles.queue}>{t.queueName}</Text><Text style={styles.date}>{t.createdAt}{t.counterName ? ` · ${t.counterName}` : ''}</Text></SqCard>)}</Screen>;
}
const styles = StyleSheet.create({ row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }, number: { color: colors.primary, fontSize: 25, fontWeight: '900' }, status: { fontWeight: '900' }, queue: { color: colors.text, marginTop: 6, fontWeight: '800', fontSize: 16 }, date: { color: colors.muted, marginTop: 4 } });
