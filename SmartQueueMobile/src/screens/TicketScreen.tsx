import React, { useCallback, useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Screen } from '../components/Screen';
import { SqCard } from '../components/SqCard';
import { SqButton } from '../components/SqButton';
import { PageHeader } from '../components/PageHeader';
import { colors } from '../theme/colors';
import { api } from '../api/api';
import { Ticket } from '../types/models';

function statusColor(status: Ticket['status']) { if (status === 'Waiting') return colors.warning; if (status === 'Called') return colors.primary; if (status === 'Done') return colors.success; if (status === 'Skipped') return colors.danger; return colors.text; }
export function TicketScreen() {
  const [ticket, setTicket] = useState<Ticket | null>(null);
  const load = useCallback(() => { api.getMyTicket().then(setTicket); }, []);
  useFocusEffect(load); useEffect(() => { const id = setInterval(load, 3000); return () => clearInterval(id); }, [load]);
  async function simulate() { setTicket(await api.simulateProgress()); }
  if (!ticket) return <Screen><PageHeader title="Moj ticket" subtitle="Aktivni broj i procjena čekanja." /><SqCard><Text style={styles.empty}>Nema aktivnog ticketa. Idi na Redovi i uzmi broj.</Text></SqCard></Screen>;
  return <Screen><PageHeader title="Moj ticket"  /><SqCard style={styles.ticketCard}><Text style={styles.queueName}>{ticket.queueName}</Text><View style={styles.ticketBadge}><Text style={styles.ticketNumber}>{ticket.number}</Text></View><Text style={[styles.status, { color: statusColor(ticket.status) }]}>{ticket.status}</Text>{ticket.counterName && <Text style={styles.counter}>▣ {ticket.counterName}</Text>}</SqCard><View style={styles.grid}><SqCard style={styles.stat}><View style={styles.statIcon}><Text style={styles.statIconText}>☷</Text></View><Text style={styles.value}>{ticket.position}</Text><Text style={styles.label}>pozicija</Text></SqCard><SqCard style={styles.stat}><View style={[styles.statIcon, { backgroundColor: 'rgba(245,166,35,0.15)' }]}><Text style={[styles.statIconText, { color: colors.warning }]}>◷</Text></View><Text style={styles.value}>{ticket.estimatedWaitMinutes} min</Text><Text style={styles.label}>procjena</Text></SqCard></View><SqCard><Text style={styles.label}>Kreirano</Text><Text style={styles.created}>{ticket.createdAt}</Text></SqCard><SqButton title="Simuliraj live pomak" onPress={simulate} /><Text style={styles.note}>Kasnije se ovaj gumb miče, a promjene dolaze preko SignalR konekcije na /hubs/queue.</Text></Screen>;
}
const styles = StyleSheet.create({ empty: { color: colors.muted, textAlign: 'center' }, ticketCard: { alignItems: 'center', gap: 14 }, queueName: { color: colors.text, fontSize: 19, fontWeight: '800' }, ticketBadge: { borderColor: colors.primary, borderWidth: 2, borderRadius: 16, paddingVertical: 26, paddingHorizontal: 50, backgroundColor: 'rgba(79,142,247,0.10)' }, ticketNumber: { color: colors.primary, fontSize: 78, fontWeight: '900', letterSpacing: -2 }, status: { fontSize: 18, fontWeight: '900' }, counter: { color: colors.text, fontSize: 16, fontWeight: '700' }, grid: { flexDirection: 'row', gap: 12 }, stat: { flex: 1 }, statIcon: { width: 44, height: 44, borderRadius: 10, backgroundColor: 'rgba(79,142,247,0.15)', alignItems: 'center', justifyContent: 'center', marginBottom: 10 }, statIconText: { color: colors.primary, fontSize: 20, fontWeight: '900' }, value: { color: colors.text, fontSize: 25, fontWeight: '900' }, label: { color: colors.muted, fontSize: 13, fontWeight: '700' }, created: { color: colors.text, marginTop: 4 }, note: { color: colors.muted, textAlign: 'center', fontSize: 12 } });
