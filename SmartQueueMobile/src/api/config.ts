import { Platform } from 'react-native';

export const API_MODE: 'mock' | 'real' = 'real';
export const API_BASE_URL = Platform.OS === 'web'
  ? 'http://localhost:5179/api'
  : 'http://10.0.2.2:5179/api';