import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Text } from 'react-native';
import { colors } from '../theme/colors';
import { useAuth } from '../context/AuthContext';
import { LoginScreen } from '../screens/LoginScreen';
import { RegisterScreen } from '../screens/RegisterScreen';
import { QueueListScreen } from '../screens/QueueListScreen';
import { TicketScreen } from '../screens/TicketScreen';
import { HistoryScreen } from '../screens/HistoryScreen';
import { ProfileScreen } from '../screens/ProfileScreen';

export type RootStackParamList = { Login: undefined; Register: undefined; Main: undefined };
export type MainTabParamList = { Redovi: undefined; Ticket: undefined; Povijest: undefined; Profil: undefined };
const Stack = createNativeStackNavigator<RootStackParamList>();
const Tab = createBottomTabNavigator<MainTabParamList>();

function MainTabs() {
  return <Tab.Navigator screenOptions={{ headerShown: false, tabBarStyle: { backgroundColor: colors.surface, borderTopColor: colors.border, height: 64, paddingBottom: 8, paddingTop: 7 }, tabBarLabelStyle: { fontWeight: '700' }, tabBarActiveTintColor: colors.primary, tabBarInactiveTintColor: colors.muted }}>
    <Tab.Screen name="Redovi" component={QueueListScreen} options={{ tabBarIcon: ({ color }) => <TabIcon color={color} label="▦" /> }} />
    <Tab.Screen name="Ticket" component={TicketScreen} options={{ tabBarIcon: ({ color }) => <TabIcon color={color} label="#" /> }} />
    <Tab.Screen name="Povijest" component={HistoryScreen} options={{ tabBarIcon: ({ color }) => <TabIcon color={color} label="◷" /> }} />
    <Tab.Screen name="Profil" component={ProfileScreen} options={{ tabBarIcon: ({ color }) => <TabIcon color={color} label="●" /> }} />
  </Tab.Navigator>;
}
function TabIcon({ color, label }: { color: string; label: string }) { return <Text style={{ color, fontWeight: '900', fontSize: 18 }}>{label}</Text>; }

export function AppNavigator() {
  const { isAuthenticated } = useAuth();
  return <Stack.Navigator screenOptions={{ headerShown: false }}>{isAuthenticated ? <Stack.Screen name="Main" component={MainTabs} /> : <><Stack.Screen name="Login" component={LoginScreen} /><Stack.Screen name="Register" component={RegisterScreen} /></>}</Stack.Navigator>;
}
