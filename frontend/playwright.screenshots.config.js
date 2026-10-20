import { defineConfig } from '@playwright/test'
import browserConfig from './playwright.config.js'

// Documentation capture is explicit and separate from the regression suite.
export default defineConfig({
  ...browserConfig,
  testDir: './screenshots',
  fullyParallel: false,
  workers: 1,
  projects: [{ name: 'portfolio', use: { browserName: 'chromium' } }],
})
