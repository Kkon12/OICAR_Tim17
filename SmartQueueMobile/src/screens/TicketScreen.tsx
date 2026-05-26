import React, { useEffect, useState, useCallback } from 'react';
import { StyleSheet, Text, View, Animated } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Screen } from '../components/Screen';
import { SqCard } from '../components/SqCard';
import { PageHeader } from '../components/PageHeader';
import { colors } from '../theme/colors';
import { api } from '../api/api';
import { Ticket } from '../types/models';

const STATUS_CONFIG = {
  Waiting: {
    label: 'Čekanje u redu',
    color: colors.warning,
    bg: 'rgba(245,166,35,0.10)',
    border: 'rgba(245,166,35,0.30)',
    icon: '◷',
  },
  Called: {
    label: '📣 Vaš red je!',
    color: '#3ecf8e',
    bg: 'rgba(62,207,142,0.12)',
    border: 'rgba(62,207,142,0.50)',
    icon: '✓',
  },
  InService: {
    label: 'U usluzi',
    color: colors.primary,
    bg: 'rgba(79,142,247,0.10)',
    border: colors.primary,
    icon: '▣',
  },
  Done: {
    label: 'Završeno',
    color: colors.muted,
    bg: 'rgba(136,146,164,0.10)',
    border: 'rgba(136,146,164,0.20)',
    icon: '✓',
  },
  Skipped: {
    label: 'Preskočeno',
    color: colors.danger,
    bg: 'rgba(231,76,60,0.10)',
    border: 'rgba(231,76,60,0.30)',
    icon: '✕',
  },
};

export function TicketScreen() {
  const [ticket, setTicket] = useState<Ticket | null>(null);

  async function load() {
    const t = await api.getMyTicket();
    setTicket(t ? { ...t } : null);
  }

  useFocusEffect(useCallback(() => { load(); }, []));
  useEffect(() => { const id = setInterval(load, 3000); return () => clearInterval(id); }, []);

  if (!ticket) {
    return (
      <Screen>
        <PageHeader title="Moj ticket" subtitle="Aktivni broj i procjena čekanja." />
        <SqCard>
          <Text style={styles.empty}>Nema aktivnog ticketa.</Text>
          <Text style={styles.emptyHint}>Idi na Redovi i uzmi broj.</Text>
        </SqCard>
      </Screen>
    );
  }

  const cfg = STATUS_CONFIG[ticket.status] ?? STATUS_CONFIG.Waiting;
  const isCalled = ticket.status === 'Called';

  return (
    <Screen>
      <PageHeader title="Moj ticket" />

      {/* ── Main ticket card — changes dramatically when Called */}
      <View style={[
        styles.mainCard,
        { borderColor: cfg.border, backgroundColor: cfg.bg },
        isCalled && styles.mainCardCalled,
      ]}>
        <Text style={styles.queueName}>{ticket.queueName}</Text>

        {/* Big ticket number */}
        <View style={[styles.numberBadge, { borderColor: cfg.color }]}>
          <Text style={[styles.numberText, { color: cfg.color }]}>
            {ticket.number}
          </Text>
        </View>

        {/* Status label */}
        <Text style={[styles.statusLabel, { color: cfg.color }]}>
          {cfg.label}
        </Text>

        {/* Counter — only shows when Called */}
        {ticket.counterName && (
          <View style={[styles.counterBadge, { borderColor: cfg.color }]}>
            <Text style={[styles.counterText, { color: cfg.color }]}>
              {ticket.counterName}
            </Text>
          </View>
        )}
      </View>

      {/* ── Stats row — hide when Done/Called */}
      {(ticket.status === 'Waiting') && (
        <View style={styles.statsRow}>
          <SqCard style={styles.stat}>
            <Text style={styles.statIcon}>☷</Text>
            <Text style={styles.statValue}>{ticket.position}</Text>
            <Text style={styles.statLabel}>pozicija u redu</Text>
          </SqCard>
          <SqCard style={styles.stat}>
            <Text style={[styles.statIcon, { color: colors.warning }]}>◷</Text>
            <Text style={styles.statValue}>{ticket.estimatedWaitMinutes}</Text>
            <Text style={styles.statLabel}>min čekanja</Text>
          </SqCard>
        </View>
      )}

      {/* ── Created at */}
      <SqCard style={styles.metaCard}>
        <Text style={styles.metaLabel}>Preuzet</Text>
        <Text style={styles.metaValue}>{ticket.createdAt}</Text>
      </SqCard>

    </Screen>
  );
}

const styles = StyleSheet.create({
  empty: { color: colors.text, textAlign: 'center', fontSize: 16, fontWeight: '700', marginBottom: 6 },
  emptyHint: { color: colors.muted, textAlign: 'center', fontSize: 14 },

  mainCard: {
    borderWidth: 2,
    borderRadius: 20,
    padding: 28,
    alignItems: 'center',
    gap: 16,
    marginBottom: 14,
  },
  mainCardCalled: {
    shadowColor: '#3ecf8e',
    shadowOpacity: 0.3,
    shadowRadius: 20,
    elevation: 8,
  },

  queueName: {
    color: colors.muted,
    fontSize: 14,
    fontWeight: '700',
    textTransform: 'uppercase',
    letterSpacing: 1.5,
  },

  numberBadge: {
    borderWidth: 3,
    borderRadius: 20,
    paddingVertical: 24,
    paddingHorizontal: 52,
  },
  numberText: {
    fontSize: 86,
    fontWeight: '900',
    letterSpacing: -3,
  },

  statusLabel: {
    fontSize: 20,
    fontWeight: '900',
    letterSpacing: 0.5,
  },

  counterBadge: {
    borderWidth: 1.5,
    borderRadius: 99,
    paddingHorizontal: 20,
    paddingVertical: 8,
    marginTop: 4,
  },
  counterText: {
    fontSize: 15,
    fontWeight: '800',
  },

  statsRow: {
    flexDirection: 'row',
    gap: 12,
    marginBottom: 14,
  },
  stat: {
    flex: 1,
    alignItems: 'center',
    gap: 6,
  },
  statIcon: {
    color: colors.primary,
    fontSize: 22,
    fontWeight: '900',
  },
  statValue: {
    color: colors.text,
    fontSize: 28,
    fontWeight: '900',
  },
  statLabel: {
    color: colors.muted,
    fontSize: 12,
    fontWeight: '700',
    textAlign: 'center',
  },

  metaCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  metaLabel: {
    color: colors.muted,
    fontSize: 13,
    fontWeight: '700',
  },
  metaValue: {
    color: colors.text,
    fontSize: 13,
    fontWeight: '700',
  },
});