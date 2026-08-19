const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  page.on('console', (m) => console.log('CONSOLE:', m.type(), m.text()));
  page.on('pageerror', (e) => console.log('PAGEERROR:', e.message));
  await page.goto('http://localhost:4200', { waitUntil: 'networkidle' });
  await page.waitForSelector('app-shell');
  const info = await page.evaluate(() => {
    const aside = document.querySelector('aside');
    const cs = getComputedStyle(aside);
    const rect = aside.getBoundingClientRect();
    const sheets = [...document.styleSheets].map((s) => s.href);
    const sel = document.querySelector('aside.fixed');
    return {
      position: cs.position,
      width: cs.width,
      right: cs.right,
      left: cs.left,
      display: cs.display,
      rect: { x: rect.x, w: rect.width },
      hasFixedClass: sel !== null,
      sheets,
    };
  });
  console.log(JSON.stringify(info, null, 2));
  await browser.close();
})();
