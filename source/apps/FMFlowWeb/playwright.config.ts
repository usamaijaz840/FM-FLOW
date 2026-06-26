import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  reporter: [['html', { open: 'never' }]],
  testDir: './tests/e2e',
  timeout: 60_000,
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [ 
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});


