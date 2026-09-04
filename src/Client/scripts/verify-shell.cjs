const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto('http://localhost:4200', { waitUntil: 'networkidle' });
  await page.waitForSelector('app-shell');
  await page.waitForTimeout(800);

  const checks = await page.evaluate(() => {
    const aside = document.querySelector('aside');
    const main = document.querySelector('main');
    const nav = document.querySelector('nav');
    const html = document.documentElement;
    const asideRect = aside.getBoundingClientRect();
    const mainRect = main.getBoundingClientRect();
    const firstLink = nav.querySelector('a');
    const icon = firstLink.querySelector('lucide-icon');
    const label = firstLink.querySelectorAll('span')[firstLink.querySelectorAll('span').length - 1];
    const iconRect = icon.getBoundingClientRect();
    const labelRect = label.getBoundingClientRect();
    return {
      dir: html.dir,
      lang: html.lang,
      sidebarOnRight: innerWidth - asideRect.right < 1,
      sidebarWidth: asideRect.width,
      mainNoOverlap: mainRect.right <= asideRect.left + 1,
      mainRightEdge: mainRect.right,
      asideLeftEdge: asideRect.left,
      iconRightOfLabel: iconRect.left >= labelRect.right,
      navCount: nav.querySelectorAll('a').length,
    };
  });

  console.log(JSON.stringify(checks, null, 2));
  await page.screenshot({ path: process.argv[2] || 'shell-1440.png' });
  await browser.close();
})();
