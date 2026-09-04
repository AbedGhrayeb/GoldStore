const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const results = {};

  await page.goto('http://localhost:4200', { waitUntil: 'networkidle' });
  await page.waitForSelector('app-shell');

  results.bannerHiddenWhileOnline = (await page.locator('[role="alert"]').count()) === 0;

  const session = await page.context().newCDPSession(page);
  await session.send('Network.enable');
  await session.send('Network.emulateNetworkConditions', {
    offline: true,
    latency: 0,
    downloadThroughput: -1,
    uploadThroughput: -1,
  });
  await page.waitForTimeout(800);

  results.bannerShownWhenOffline = (await page.locator('[role="alert"]').count()) === 1;
  results.bannerText = (await page.locator('[role="alert"]').innerText())
    .replace(/\s+/g, ' ')
    .trim();
  results.retryButtonPresent = (await page.locator('[role="alert"] button').count()) === 1;

  await session.send('Network.emulateNetworkConditions', {
    offline: false,
    latency: 0,
    downloadThroughput: -1,
    uploadThroughput: -1,
  });
  await page.waitForTimeout(800);

  results.bannerHiddenWhenBackOnline = (await page.locator('[role="alert"]').count()) === 0;

  console.log(JSON.stringify(results, null, 2));
  await browser.close();
})();
