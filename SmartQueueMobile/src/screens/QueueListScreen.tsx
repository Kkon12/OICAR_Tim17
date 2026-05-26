import React, { useEffect, useState } from "react";
import { Alert, Pressable, StyleSheet, Text, View } from "react-native";
import { useNavigation } from "@react-navigation/native";
import { BottomTabNavigationProp } from "@react-navigation/bottom-tabs";
import { Screen } from "../components/Screen";
import { PageHeader } from "../components/PageHeader";
import { SqButton } from "../components/SqButton";
import { colors } from "../theme/colors";
import { api } from "../api/api";
import { QueueItem } from "../types/models";
import { MainTabParamList } from "../navigation/AppNavigator";
import { useAuth } from "../context/AuthContext";

type Navigation = BottomTabNavigationProp<MainTabParamList>;

export function QueueListScreen() {
  const navigation = useNavigation<Navigation>();
  const { user } = useAuth();
  const [queues, setQueues] = useState<QueueItem[]>([]);
  const [selectedQueueId, setSelectedQueueId] = useState<number | null>(null);
  const [loadingId, setLoadingId] = useState<number | null>(null);

  useEffect(() => {
    api
      .getQueues()
      .then(setQueues)
      .catch((e) => Alert.alert("Greška", e.message));
  }, []);

  const selectedQueue =
    queues.find((queue) => queue.id === selectedQueueId) ??
    queues.find((queue) => queue.isActive) ??
    null;

  async function takeTicket(queue: QueueItem) {
    setLoadingId(queue.id);
    try {
      // ── Pass userId so the ticket is linked to the logged-in user
      // This makes it show up in GET /api/ticket/my
      const ticket = await api.takeTicket(queue.id, user?.id);
      Alert.alert("Broj uspješno preuzet", `Tvoj broj je ${ticket.number}.`);
      navigation.navigate("Ticket");
    } catch (e) {
      Alert.alert(
        "Greška",
        e instanceof Error ? e.message : "Ne mogu uzeti ticket.",
      );
    } finally {
      setLoadingId(null);
    }
  }

  return (
    <Screen>
      <View style={styles.kioskHeader}>
        <Text style={styles.logo}>▦</Text>
        <Text style={styles.brand}>SmartQueue</Text>
        <Text style={styles.kioskSub}>Odaberite uslugu i preuzmite broj</Text>
      </View>

      <PageHeader title="Kiosk / Redovi čekanja" />

      {selectedQueue && (
        <View style={styles.takeNumberPanel}>
          <Text style={styles.panelEyebrow}>Odabrana usluga</Text>
          <Text style={styles.panelTitle}>{selectedQueue.name}</Text>
          <Text style={styles.panelText}>
            Trenutni broj: {selectedQueue.currentNumber} · Čeka:{" "}
            {selectedQueue.waitingCount} · Procjena:{" "}
            {selectedQueue.waitingCount * selectedQueue.averageWaitMinutes} min
          </Text>
          <SqButton
            title={loadingId === selectedQueue.id ? "Kreiram broj..." : "UZMI BROJ"}
            onPress={() => takeTicket(selectedQueue)}
            disabled={!selectedQueue.isActive || loadingId === selectedQueue.id}
          />
        </View>
      )}

      <Text style={styles.sectionTitle}>Dostupni redovi</Text>
      <View style={styles.grid}>
        {queues.map((queue) => {
          const selected = queue.id === selectedQueue?.id;
          return (
            <Pressable
              key={queue.id}
              disabled={!queue.isActive || loadingId === queue.id}
              onPress={() => setSelectedQueueId(queue.id)}
              style={({ pressed }) => [
                styles.queueCard,
                selected && styles.selectedCard,
                pressed && queue.isActive && styles.pressed,
                !queue.isActive && styles.disabled,
              ]}
            >
              <View style={styles.queueIcon}>
                <Text style={styles.queueIconText}>☷</Text>
              </View>
              <Text style={styles.queueName}>{queue.name}</Text>
              <Text style={styles.desc}>{queue.description}</Text>
              <View style={styles.metaRow}>
                <Text style={[styles.wait, queue.waitingCount === 0 && { color: colors.success }]}>
                  {queue.waitingCount === 0 ? "✓ Nema čekanja" : `☷ ${queue.waitingCount} čeka`}
                </Text>
              </View>
              <Text style={[styles.counters, queue.openCounters === 0 && { color: colors.muted }]}>
                {queue.openCounters > 0 ? `▣ otvorenih šaltera: ${queue.openCounters}` : "Zatvoreno"}
              </Text>
              <View style={[styles.badge, queue.isActive ? styles.active : styles.closed]}>
                <Text style={styles.badgeText}>{queue.isActive ? "Aktivan" : "Zatvoren"}</Text>
              </View>
              <View style={styles.cardButtonWrap}>
                <SqButton
                  title={loadingId === queue.id ? "..." : selected ? "ODABRANO" : "ODABERI"}
                  variant={selected ? "primary" : "ghost"}
                  disabled={!queue.isActive || loadingId === queue.id}
                  onPress={() => setSelectedQueueId(queue.id)}
                />
              </View>
            </Pressable>
          );
        })}
      </View>

      <Text style={styles.footer}>
        SmartQueue © 2026 — admin i djelatnik ostaju na web aplikaciji
      </Text>
    </Screen>
  );
}

const styles = StyleSheet.create({
  kioskHeader: { paddingVertical: 20, alignItems: "center", borderBottomWidth: 1, borderBottomColor: colors.border, marginBottom: 10 },
  logo: { color: colors.primary, fontSize: 34, fontWeight: "900" },
  brand: { color: colors.text, fontSize: 30, fontWeight: "900", marginTop: 4 },
  kioskSub: { color: colors.muted, marginTop: 4 },
  takeNumberPanel: { backgroundColor: colors.surface, borderWidth: 2, borderColor: colors.primary, borderRadius: 18, padding: 20, marginBottom: 18, gap: 10, shadowColor: "#000", shadowOpacity: 0.25, shadowRadius: 12, elevation: 4 },
  panelEyebrow: { color: colors.primary, fontSize: 12, fontWeight: "900", textTransform: "uppercase", letterSpacing: 1 },
  panelTitle: { color: colors.text, fontSize: 24, fontWeight: "900" },
  panelText: { color: colors.muted, lineHeight: 20, marginBottom: 4 },
  sectionTitle: { color: colors.text, fontSize: 18, fontWeight: "900", marginBottom: 12 },
  grid: { gap: 16 },
  queueCard: { backgroundColor: colors.surface, borderWidth: 2, borderColor: colors.border, borderRadius: 16, padding: 22, alignItems: "center", gap: 9 },
  selectedCard: { borderColor: colors.primary, backgroundColor: "rgba(79,142,247,0.06)" },
  pressed: { transform: [{ translateY: -3 }], backgroundColor: "rgba(79,142,247,0.05)" },
  disabled: { opacity: 0.55 },
  queueIcon: { width: 64, height: 64, borderRadius: 16, backgroundColor: "rgba(79,142,247,0.12)", alignItems: "center", justifyContent: "center" },
  queueIconText: { color: colors.primary, fontSize: 26, fontWeight: "900" },
  queueName: { color: colors.text, fontSize: 20, fontWeight: "800", textAlign: "center" },
  desc: { color: colors.muted, textAlign: "center", fontSize: 13 },
  metaRow: { marginTop: 4 },
  wait: { color: colors.muted, fontWeight: "700" },
  counters: { color: colors.success, fontSize: 13, fontWeight: "700" },
  badge: { marginTop: 8, borderRadius: 99, paddingHorizontal: 12, paddingVertical: 5 },
  active: { backgroundColor: "rgba(62,207,142,0.12)" },
  closed: { backgroundColor: "rgba(136,146,164,0.12)" },
  badgeText: { color: colors.text, fontSize: 12, fontWeight: "800" },
  cardButtonWrap: { width: "100%", marginTop: 10 },
  footer: { color: colors.muted, textAlign: "center", fontSize: 12, marginTop: 6 },
});