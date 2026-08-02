/* ═══════════════════════════════════════════════════════
   HOME DASHBOARD — JavaScript
   Loads live data for the dashboard KPI cards, inventory
   trend chart, per-employee stats and recent transactions.
   ═══════════════════════════════════════════════════════ */

$(document).ready(function () {
    initHome();
});

function initHome() {
    loadGoldPriceCard();
    loadGoldWeightCard();
    loadAccountBalanceCard('cash');
    loadAccountBalanceCard('bank');
    loadEmployeeStats();
    loadRecentTransactions();
    initTrendTabs();
    loadGoldTrend('week');
}

// ── Card 1: Global Gold Price ─────────────────────────
async function loadGoldPriceCard() {
    var valueEl = document.getElementById('goldPriceValue');
    var unitEl = document.getElementById('goldPriceUnit');
    var changeEl = document.getElementById('goldPriceChange');
    if (!valueEl || !unitEl || !changeEl) return;

    var data = await loadGoldPrices();
    if (!data || !data.pricePerGram21K) return;

    var price = data.pricePerGram21K;
    valueEl.textContent = price.displayPrice || formatWeight(price.price);
    unitEl.textContent = price.currencySymbol || '';
    changeEl.innerHTML = renderGoldPriceChange(price.changePercent24H, price.changeDirection);
}

// ── Card 2: Total Gold Weight (21K equivalent) ────────
async function loadGoldWeightCard() {
    var valueEl = document.getElementById('goldWeightValue');
    var unitEl = document.getElementById('goldWeightUnit');
    var subEl = document.getElementById('goldWeightSub');
    if (!valueEl || !unitEl || !subEl) return;

    var data = await loadInventoryKpis();
    if (!data) return;

    valueEl.textContent = data.totalEquivalent21KDisplay || formatWeight(data.totalEquivalent21KGrams);
    unitEl.textContent = data.totalEquivalent21KUnit || 'جم';

    var parts = (data.karatBreakdowns || [])
        .filter(function (b) { return b.totalWeightGrams > 0; })
        .map(function (b) { return b.karatLabel + ' ' + b.totalWeightDisplay + ' جم'; });
    subEl.textContent = parts.join(' · ');
}

// ── Cards 3 & 4: Cash / Bank balances per currency ────
function loadAccountBalanceCard(kind) {
    var url = kind === 'cash' ? '/FinancialAccounts/GetCashAccounts' : '/FinancialAccounts/GetBankAccounts';
    var containerId = kind === 'cash' ? 'cashBalanceRows' : 'bankBalanceRows';

    $.ajax({
        url: url,
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var container = document.getElementById(containerId);
            if (!container || !Array.isArray(data)) return;

            var totals = aggregateByCurrency(data);
            if (totals.length === 0) {
                container.innerHTML = emptyBalanceRow();
                return;
            }

            container.innerHTML = '';
            totals.forEach(function (t) {
                container.innerHTML += balanceRow(t);
            });
        },
        error: function () {
            console.error('Failed to load ' + kind + ' balances');
        }
    });
}

function aggregateByCurrency(accounts) {
    var map = {};
    accounts.forEach(function (a) {
        if (!map[a.currency]) {
            map[a.currency] = { currency: a.currency, symbol: a.currencySymbol || a.currency, balance: 0 };
        }
        map[a.currency].balance += parseFloat(a.balance) || 0;
    });
    var order = ['JOD', 'USD', 'ILS'];
    return order
        .filter(function (code) { return map[code]; })
        .map(function (code) { return map[code]; });
}

function balanceRow(t) {
    return '<div class="flex items-center justify-between gap-2">' +
        '<span class="text-xs text-secondary">' + escapeHtml(t.currency) + '</span>' +
        '<span class="text-sm font-bold text-on-surface font-data-mono" dir="ltr">' + formatMoney(t.balance) + ' ' + escapeHtml(t.symbol) + '</span>' +
    '</div>';
}

function emptyBalanceRow() {
    return '<div class="flex items-center justify-between gap-2">' +
        '<span class="text-xs text-secondary">لا أرصدة</span>' +
    '</div>';
}

// ── Employee Today Stats ──────────────────────────────
function loadEmployeeStats() {
    $.ajax({
        url: '/StoreOperations/GetTodayEmployeeStats',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var container = document.getElementById('employeeStats');
            if (!container) return;

            if (!Array.isArray(data) || data.length === 0) {
                container.innerHTML = '<p class="text-sm text-secondary text-center py-6">لا توجد عمليات لليوم</p>';
                return;
            }

            container.innerHTML = '';
            data.forEach(function (emp) {
                container.innerHTML += employeeStatRow(emp);
            });
        },
        error: function () {
            console.error('Failed to load employee stats');
        }
    });
}

function employeeStatRow(emp) {
    var salesMoney = (emp.salesTotals || []).map(moneyLabel).join(' · ');
    var purchasesMoney = (emp.purchasesTotals || []).map(moneyLabel).join(' · ');

    return '<div class="rounded-xl border border-outline-variant bg-surface-container-lowest p-3 flex flex-col gap-2">' +
        '<div class="flex items-center justify-between gap-2">' +
            '<div class="flex items-center gap-2 min-w-0">' +
                '<div class="w-8 h-8 rounded-full bg-primary-container flex items-center justify-center text-on-primary-container font-semibold text-xs shrink-0">' + employeeInitial(emp.employeeName) + '</div>' +
                '<p class="stat-label truncate">' + escapeHtml(emp.employeeName) + '</p>' +
            '</div>' +
            '<p class="text-xs text-secondary shrink-0">' + emp.salesCount + ' بيع · ' + emp.purchasesCount + ' شراء</p>' +
        '</div>' +
        '<div class="grid grid-cols-2 gap-2">' +
            '<div class="rounded-lg bg-surface-container p-2 flex flex-col gap-1">' +
                '<p class="text-xs text-secondary">مبيعات (21ك)</p>' +
                '<p class="text-sm font-bold text-primary font-data-mono">' + formatWeight(emp.salesWeight21K) + ' جم</p>' +
                (salesMoney ? '<p class="text-xs text-secondary font-data-mono">' + salesMoney + '</p>' : '') +
            '</div>' +
            '<div class="rounded-lg bg-surface-container p-2 flex flex-col gap-1">' +
                '<p class="text-xs text-secondary">مشتريات (21ك)</p>' +
                '<p class="text-sm font-bold text-gray-500 font-data-mono">' + formatWeight(emp.purchasesWeight21K) + ' جم</p>' +
                (purchasesMoney ? '<p class="text-xs text-secondary font-data-mono">' + purchasesMoney + '</p>' : '') +
            '</div>' +
        '</div>' +
    '</div>';
}

function moneyLabel(t) {
    return escapeHtml(t.currency) + ' ' + escapeHtml(t.symbol) + ' ' + formatMoney(t.amount);
}

function employeeInitial(name) {
    if (!name) return '؟';
    return escapeHtml(name.trim().charAt(0));
}

// ── Recent Financial Transactions ─────────────────────
function loadRecentTransactions() {
    $.ajax({
        url: '/FinancialAccounts/GetRecentTransactions?count=10',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var tbody = document.getElementById('recent-transactions-body');
            if (!tbody) return;

            if (!data || data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5" class="py-12 text-center text-secondary">لا توجد حركات</td></tr>';
                return;
            }

            tbody.innerHTML = '';
            data.forEach(function (t) {
                var amountClass = t.transactionType === 'Inflow' ? 'text-tertiary' : 'text-error';
                var amountPrefix = t.transactionType === 'Inflow' ? '+' : '-';
                var statusLabel = t.transactionType === 'Inflow' ? 'إيداع' : 'سحب';
                var statusClass = t.transactionType === 'Inflow' ? 'bg-tertiary-container text-tertiary' : 'bg-error-container text-error';

                tbody.innerHTML += '<tr class="border-b border-outline-variant hover:bg-surface-container-low transition-colors">' +
                    '<td class="px-md py-3 font-data-mono text-secondary">' + escapeHtml(t.date) + '</td>' +
                    '<td class="px-md py-3">' + escapeHtml(t.description) + '</td>' +
                    '<td class="px-md py-3 text-secondary">' + escapeHtml(t.accountName) + '</td>' +
                    '<td class="px-md py-3 font-data-mono ' + amountClass + '" dir="ltr">' + amountPrefix + ' ' + formatMoney(t.amount) + '</td>' +
                    '<td class="px-md py-3 text-center">' +
                        '<span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-bold ' + statusClass + '">' + statusLabel + '</span>' +
                    '</td>' +
                '</tr>';
            });
        },
        error: function () {
            console.error('Failed to load recent transactions');
        }
    });
}

// ── Gold Inventory Trend Chart ────────────────────────
var trendChart = null;

function initTrendTabs() {
    var tabs = document.getElementById('trendTabs');
    if (!tabs) return;

    tabs.querySelectorAll('.tab-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            tabs.querySelectorAll('.tab-btn').forEach(function (b) { b.classList.remove('active'); });
            btn.classList.add('active');
            loadGoldTrend(btn.getAttribute('data-period'));
        });
    });
}

async function loadGoldTrend(period) {
    var canvas = document.getElementById('goldTrendChart');
    if (!canvas) return;

    var days = period === 'month' ? 30 : 7;
    var data = null;
    try {
        var res = await fetch('/Inventory/GetTrend?days=' + days);
        var json = await res.json();
        if (json && json.success === false) return;
        data = json;
    } catch (e) {
        console.error('Failed to load gold trend', e);
        return;
    }

    if (!Array.isArray(data)) return;

    var labels = data.map(function (p) { return p.label; });
    var net = data.map(function (p) { return parseFloat(p.net21K) || 0; });
    var hasData = net.some(function (v) { return v !== 0; });

    if (trendChart) {
        trendChart.destroy();
        trendChart = null;
    }

    var ctx = canvas.getContext('2d');
    trendChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'صافي الوزن (21ك)',
                data: net,
                borderColor: '#D4AF37',
                backgroundColor: 'rgba(212, 175, 55, 0.12)',
                borderWidth: 2,
                fill: true,
                tension: 0.3,
                pointRadius: 2,
                pointBackgroundColor: '#D4AF37'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return formatWeight(context.parsed.y) + ' جم';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(0,0,0,0.05)' },
                    ticks: {
                        callback: function (value) { return formatWeight(value); },
                        font: { size: 11 }
                    }
                },
                x: {
                    grid: { display: false },
                    ticks: { font: { size: 11 } }
                }
            }
        }
    });

    if (!hasData) {
        chartEmptyOverlay(canvas.parentElement, 'لا توجد حركة ذهب في هذه الفترة');
    } else {
        removeChartEmptyOverlay(canvas.parentElement);
    }
}

function chartEmptyOverlay(container, text) {
    removeChartEmptyOverlay(container);
    var overlay = document.createElement('div');
    overlay.className = 'absolute inset-0 flex items-center justify-center text-sm text-secondary pointer-events-none';
    overlay.textContent = text;
    overlay.id = 'goldTrendEmptyOverlay';
    container.appendChild(overlay);
}

function removeChartEmptyOverlay(container) {
    var existing = document.getElementById('goldTrendEmptyOverlay');
    if (existing) existing.remove();
}

// ── Utility ───────────────────────────────────────────
function formatMoney(num) {
    if (num === null || num === undefined) return '0.00';
    return parseFloat(num).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatWeight(num) {
    if (num === null || num === undefined) return '0.000';
    return parseFloat(num).toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
}

function escapeHtml(str) {
    if (str === null || str === undefined) return '';
    var div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
}
