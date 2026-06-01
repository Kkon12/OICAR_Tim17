module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  testMatch: ['**/__tests__/**/*.test.ts'],
  clearMocks: true,
  moduleNameMapper: {
    '^react-native$': '<rootDir>/__mocks__/react-native.ts',
    '^@react-native-async-storage/async-storage$': '<rootDir>/__mocks__/async-storage.ts'
  },
  globals: {
    'ts-jest': {
      tsconfig: {
        jsx: 'react-jsx',
        module: 'commonjs',
        moduleResolution: 'node',
        esModuleInterop: true,
        allowSyntheticDefaultImports: true,
        strict: false
      }
    }
  }
};
