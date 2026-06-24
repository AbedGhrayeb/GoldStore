/* ═══════════════════════════════════════════════════════
   BARIQ GOLD — app.js
   Chart.js integration + table rendering + interactions
════════════════════════════════════════════════════════ */

/* ── Gold price data sets ─────────────────────────── */
const PRICE_DATA = {
  day: {
    labels: ['08:00','09:00','10:00','11:00','12:00','13:00','14:00','15:00','16:00','17:00','18:00'],
    values: [62.55, 62.70, 62.85, 62.95, 63.10, 63.05, 63.25, 63.45, 63.60, 63.50, 63.30],
  },
  week: {
    labels: ['الأحد','الاثنين','الثلاثاء','الأربعاء','الخميس','الجمعة','السبت'],
    values: [62.00, 62.40, 63.10, 62.80, 63.50, 63.20, 63.60],
  },
  month: {
    labels: ['أسبوع 1','أسبوع 2','أسبوع 3','أسبوع 4'],
    values: [60.50, 61.80, 62.90, 63.50],
  },
};

/* ── Transactions data ────────────────────────────── */
const TRANSACTIONS = [
  { ref: 'TRX-0092', type: 'sell',  label: 'بيع',       client: 'مجوهرات الأمانة', weight: 120.5, value: 'JOD 5,400', date: 'اليوم 10:30 ص', status: 'complete' },
  { ref: 'TRX-0091', type: 'buy',   label: 'شراء كسر',  client: 'أحمد حسن',        weight: 45.0,  value: 'JOD 1,850', date: 'اليوم 09:15 ص', status: 'review'   },
  { ref: 'TRX-0090', type: 'trans', label: 'تحويل رصيد', client: 'الفرع الرئيسي',  weight: 500.0, value: '—',         date: 'أمس 14:20 م',  status: 'complete' },
  { ref: 'TRX-0089', type: 'sell',  label: 'بيع',        client: 'سلمى العمري',    weight: 22.5,  value: 'JOD 920',   date: 'أمس 11:00 ص',  status: 'complete' },
  { ref: 'TRX-0088', type: 'buy',   label: 'شراء ذهب',   client: 'مورد الخليج',    weight: 200.0, value: 'JOD 8,200', date: 'أمس 09:45 ص',  status: 'review'   },
];

/* ─────────────────────────────────────────────────── */
/*  CHART                                              */
/* ─────────────────────────────────────────────────── */
let goldChart = null;
let activePeriod = 'day';

function buildChart(period) {
  const ctx = document.getElementById('goldChart');
  if (!ctx) return;

  const { labels, values } = PRICE_DATA[period];

  if (goldChart) {
    goldChart.data.labels = labels;
    goldChart.data.datasets[0].data = values;
    goldChart.update('active');
    return;
  }

  goldChart = new Chart(ctx, {
    type: 'line',
    data: {
      labels,
      datasets: [{
        data: values,
        borderColor: '#d4af37',
        borderWidth: 2.5,
        backgroundColor: (context) => {
          const chart = context.chart;
          const { ctx: c, chartArea } = chart;
          if (!chartArea) return 'transparent';
          const gradient = c.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
          gradient.addColorStop(0, 'rgba(212,175,55,0.28)');
          gradient.addColorStop(1, 'rgba(212,175,55,0.01)');
          return gradient;
        },
        fill: true,
        tension: 0.42,
        pointRadius: 4,
        pointBackgroundColor: '#ffffff',
        pointBorderColor: '#d4af37',
        pointBorderWidth: 2,
        pointHoverRadius: 6,
        pointHoverBackgroundColor: '#d4af37',
        pointHoverBorderColor: '#fff',
        pointHoverBorderWidth: 2,
      }],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: {
          rtl: true,
          titleFont: { family: 'IBM Plex Sans Arabic', size: 12 },
          bodyFont:  { family: 'IBM Plex Sans Arabic', size: 13 },
          backgroundColor: '#fff',
          titleColor: '#5f5e5e',
          bodyColor: '#1a1b22',
          borderColor: '#d0c5af',
          borderWidth: 1,
          padding: 10,
          callbacks: {
            label: (ctx) => ` ${ctx.parsed.y.toFixed(2)} د.أ / جم`,
          },
        },
      },
      scales: {
        x: {
          grid: { display: false },
          border: { display: false },
          ticks: {
            font: { family: 'IBM Plex Sans Arabic', size: 11 },
            color: '#7f7663',
            maxRotation: 0,
          },
        },
        y: {
          position: 'right',
          grid: { color: '#e8e4db', drawBorder: false },
          border: { display: false, dash: [4, 4] },
          ticks: {
            font: { family: 'IBM Plex Sans Arabic', size: 11 },
            color: '#7f7663',
            callback: (v) => v.toFixed(1),
          },
        },
      },
    },
  });
}

/* ─────────────────────────────────────────────────── */
/*  TRANSACTIONS TABLE                                 */
/* ─────────────────────────────────────────────────── */
function typeClass(type) {
  return { sell: 'tx-type-sell', buy: 'tx-type-buy', trans: 'tx-type-trans' }[type] ?? '';
}

function typeArrow(type) {
  return { sell: '↓', buy: '↑', trans: '⇄' }[type] ?? '';
}

function badgeHTML(status) {
  return status === 'complete'
    ? '<span class="badge-complete">مكتمل</span>'
    : '<span class="badge-review">قيد المراجعة</span>';
}

function renderTable() {
  const tbody = document.getElementById('tx-table-body');
  if (!tbody) return;

  tbody.innerHTML = TRANSACTIONS.map((tx) => `
    <tr class="hover:bg-surface transition-colors">
      <td class="font-mono text-xs text-muted">${tx.ref}</td>
      <td class="${typeClass(tx.type)}">${typeArrow(tx.type)} ${tx.label}</td>
      <td>${tx.client}</td>
      <td class="text-left font-medium">${tx.weight.toFixed(1)}</td>
      <td class="text-left font-semibold">${tx.value}</td>
      <td class="text-muted text-xs whitespace-nowrap">${tx.date}</td>
      <td>${badgeHTML(tx.status)}</td>
    </tr>
  `).join('');
}

/* ─────────────────────────────────────────────────── */
/*  TAB BUTTONS                                        */
/* ─────────────────────────────────────────────────── */
function initTabs() {
  document.querySelectorAll('.tab-btn').forEach((btn) => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.tab-btn').forEach((b) => b.classList.remove('active'));
      btn.classList.add('active');
      activePeriod = btn.dataset.period;
      buildChart(activePeriod);
    });
  });
}

/* ─────────────────────────────────────────────────── */
/*  SIDEBAR (mobile)                                   */
/* ─────────────────────────────────────────────────── */
function openSidebar() {
  const sidebar  = document.getElementById('sidebar');
  const overlay  = document.getElementById('sidebar-overlay');
  sidebar.classList.remove('translate-x-full');
  overlay.classList.remove('hidden');
  document.body.style.overflow = 'hidden';
}

function closeSidebar() {
  const sidebar  = document.getElementById('sidebar');
  const overlay  = document.getElementById('sidebar-overlay');
  sidebar.classList.add('translate-x-full');
  overlay.classList.add('hidden');
  document.body.style.overflow = '';
}

/* close sidebar when nav link is clicked on mobile + persist active state on load */
function initNavLinks() {
  const navLinks = document.querySelectorAll('.nav-link');

  /* restore active state after full page navigation by matching the controller segment in the URL */
  const pathSegments = window.location.pathname.split('/').filter(Boolean);
  const currentController = (pathSegments[0] || 'Home').toLowerCase();

  navLinks.forEach((link) => {
    const href = link.getAttribute('href');
    if (!href || href === '#') return;
    const linkSegments = href.split('/').filter(Boolean);
    const linkController = (linkSegments[0] || 'Home').toLowerCase();
    if (linkController === currentController) {
      link.classList.add('active');
    }
  });

  navLinks.forEach((link) => {
    link.addEventListener('click', (e) => {
      //e.preventDefault();
      if (window.innerWidth < 1024) closeSidebar();
      document.querySelectorAll('.nav-link').forEach((l) => l.classList.remove('active'));
      link.classList.add('active');
    });
  });
}

/* ─────────────────────────────────────────────────── */
/*  PROGRESS BARS — animate on load                   */
/* ─────────────────────────────────────────────────── */
function animateBars() {
  document.querySelectorAll('.progress-bar').forEach((bar) => {
    const target = bar.style.width;
    bar.style.width = '0%';
    requestAnimationFrame(() => {
      setTimeout(() => { bar.style.width = target; }, 100);
    });
  });
}

/* ─────────────────────────────────────────────────── */
/*  LIVE CLOCK (optional header enhancement)          */
/* ─────────────────────────────────────────────────── */
function startClock() {
  const el = document.getElementById('live-clock');
  if (!el) return;
  const tick = () => {
    const now = new Date();
    el.textContent = now.toLocaleTimeString('ar-SA', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  };
  tick();
  setInterval(tick, 1000);
}

/* ─────────────────────────────────────────────────── */
/*  INIT                                               */
/* ─────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
  buildChart('day');
  renderTable();
  initTabs();
  initNavLinks();
  animateBars();
  startClock();

  /* expose sidebar helpers to onclick attributes in HTML */
  window.openSidebar  = openSidebar;
  window.closeSidebar = closeSidebar;
});
