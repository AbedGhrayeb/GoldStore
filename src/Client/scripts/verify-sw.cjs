const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const results = {};

  await page.goto('http://localhost:4300', { waitUntil: 'networkidle' });

  const swState = await page.evaluate(async () => {
    const reg = await navigator.serviceWorker.getRegistration();
    return reg ? { active: !!reg.active, state: reg.active?.state ?? null } : null;
  });
  results.registration = swState;

  await page.reload({ waitUntil: 'networkidle' });
  results.controlled = await page.evaluate(() => navigator.serviceWorker.controller !== null);

  const session = await page.context().newCDPSession(page);
  await session.send('Network.enable');
  await session.send('Network.emulateNetworkConditions', {
    offline: true,
    latency: 0,
    downloadThroughput: -1,
    uploadThroughput: -1,
  });

  const reloadOk = await page
    .reload({ waitUntil: 'domcontentloaded', timeout: 5000 })
    .then(() => true)
    .catch(() => false);
  await page.waitForTimeout(800);
  results.offlineReloadOk = reloadOk;
  results.shellRendered = reloadOk ? (await page.locator('app-shell').count()) === 1 : false;
  results.navigatorOnLine = await page.evaluate(() => navigator.onLine);
  results.bannerShown = reloadOk ? (await page.locator('[role="alert"]').count()) === 1 : false;
  results.bannerCount = await page
    .locator('[role="alert"]')
    .count()
    .catch(() => -1);

  console.log(JSON.stringify(results, null, 2));
  await browser.close();
})();
