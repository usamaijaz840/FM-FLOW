import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  reporter: [['html', { open: 'never' }]],
  testDir: './tests/components',
  timeout: 60_000,
  use: {
    baseURL: 'http://localhost:6006',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  webServer: {
    command: 'npm run storybook',
    url: 'http://localhost:6006',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    cwd: '../..',
  },
  projects: [ 
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
