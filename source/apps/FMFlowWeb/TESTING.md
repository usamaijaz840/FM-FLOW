## FMFlowWeb Testing Guide

### Prerequisites
- Node 20+ installed
- Install deps: `npm ci`
- App running locally at `http://localhost:3000` (Playwright `baseURL`)

### Scripts
- Run E2E tests: `npm run test:e2e`
- Headed mode: `npm run test:e2e:headed`
- Debug mode: `npm run test:e2e:debug`
- UI mode: `npm run test:e2e:ui`
- Open HTML report: `npm run test:e2e:report`
- Install browsers: `npm run test:e2e:install`
- Codegen (record flows): `npm run test:e2e:codegen`
- Run specific test: `npx playwright test tests/e2e/onboarding.spec.ts`

#### Raw equivalents
- `npm run test:e2e` → `npx playwright test`
- `npm run test:e2e:headed` → `npx playwright test --headed`
- `npm run test:e2e:debug` → `npx playwright test --debug`
- `npm run test:e2e:ui` → `npx playwright test --ui`
- `npm run test:e2e:report` → `npx playwright show-report`
- `npm run test:e2e:install` → `npx playwright install`
- `npm run test:e2e:codegen` → `npx playwright codegen http://localhost:3000`

### Where tests live
- E2E specs: `tests/e2e/`
- Page Objects: `tests/pages/`
- Test utilities: `tests/utils/`

### Page Object Model (POM)
- Purpose: centralize selectors and UI actions to make tests readable and resilient.
- Pattern: one class per page/step with intentful methods.

Example usage (excerpt from onboarding):
```ts
import { PersonalInformationPage } from '../pages/PersonalInformationPage';

test('Onboarding - personal info', async ({ page }) => {
  const personal = new PersonalInformationPage(page);
  await personal.gotoAndWaitForCore();
  await personal.fillRequiredFields();
  await personal.selectState('UT', 'Utah (UT)');
  await personal.selectTimeZone('Moun', 'Mountain Time (US & Canada)');
  await personal.acceptTermsAndContinue();
});
```

### Network mocking and request capture
- Keep route setup in the spec (not in page objects).
- Use `page.route` to stub endpoints and `RequestRecorder` to capture payloads.

Snippet:
```ts
await page.route('**/api/login/save-password', async (route) => {
  // record payload...
  await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
});
```

### Writing new tests
1) Create a spec under `tests/e2e/yourFeature.spec.ts`.
2) If the feature reuses onboarding or other flows, prefer existing page objects; otherwise, add a new class in `tests/pages/`.
3) Keep business assertions in the spec; put only UI actions and minimal invariant checks in POs.
4) Stub backend calls that are not critical to the UI behavior you’re asserting.

#### Minimal spec template
```ts
import { test, expect } from '@playwright/test';

test.describe('Feature X', () => {
  test('does Y', async ({ page }) => {
    // Arrange: routes/mocks if needed
    await page.route('**/api/endpoint', (route) => route.fulfill({ status: 200, body: '{}' }));

    // Act: use a page object or direct locators
    await page.goto('/path');
    await page.getByRole('button', { name: /start/i }).click();

    // Assert: user-visible outcome
    await expect(page.getByText('Success')).toBeVisible();
  });
});
```

### Local workflow
1) Start the app: `npm run dev` (port 3000).
2) In another terminal: `npm run test:e2e`.
3) View the report if something fails: `npx playwright show-report`.

### Tips
- Prefer `getByRole`/`getByLabel`; add `data-testid` for brittle targets.
- Small, focused page objects are easier to maintain than a monolith.
- Avoid putting test data generation or API stubs in page objects.

### Selector strategy
- Use accessible queries first: `getByRole`, `getByLabel`, `getByPlaceholder`.
- Add stable `data-testid` when roles/labels are not practical; avoid deep CSS/XPath.
- Prefer text assertions on headings and CTAs to verify navigation/state.

### Running subsets
- By file: `npx playwright test tests/e2e/onboarding.spec.ts`
- By title (regex): `npx playwright test -g "Referral"`
- By project/browser: `npx playwright test --project=chromium`

### Debugging & troubleshooting
- Headed + slow-mo: `npx playwright test --headed --timeout=0 --config=playwright.config.ts`
- Pause at runtime: add `await page.pause()` or run `--debug`.
- Traces: open failures with `npx playwright show-report` (traces enabled on first retry).
- Network issues: verify mocked routes are registered before navigation; use `page.waitForRequest/Response` to ensure calls happened.

### Common mistakes
- Flaky selectors: switch to role/label or add `data-testid`.
- Racing UI: wait for headings/sections, then interact; avoid arbitrary `waitForTimeout`.
- Hidden network stubs in POs: keep them in specs for clarity and reuse.
- Overspecified assertions: assert user-visible outcomes, not implementation details.


