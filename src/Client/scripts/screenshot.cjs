const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto('http://localhost:4200', { waitUntil: 'networkidle' });
  await page.waitForSelector('app-shell');
  await page.waitForTimeout(800);
  await page.screenshot({ path: process.argv[2] || 'shell-1440.png', fullPage: false });
  await browser.close();
})();
