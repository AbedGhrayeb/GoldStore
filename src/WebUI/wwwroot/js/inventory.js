/* ─── Inventory JS ─────────────────────────── */

let currentPage = 1;
const pageSize = 20;

function formatWeight(grams) {
    if (grams >= 1000) {
        return (grams / 1000).toFixed(3);
    }
    return grams.toFixed(3);
}

function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString('ar-JO', { year: 'numeric', month: '2-digit', day: '2-digit' }) +
        ' ' + d.toLocaleTimeString('ar-JO', { hour: '2-digit', minute: '2-digit' });
}

function renderChangeBadge(change, direction) {
    if (direction === 'up') {
        return `<span class="flex items-center text-tertiary-container bg-tertiary-container/10 px-2 py-1 rounded"><span class="material-symbols-outlined text-sm">trending_up</span>+${change.toFixed(2)}%</span>`;
    } else if (direction === 'down') {
        return `<span class="flex items-center text-error bg-error-container/30 px-2 py-1 rounded"><span class="material-symbols-outlined text-sm">trending_down</span>${change.toFixed(2)}%</span>`;
    }
    return `<span class="flex items-center text-secondary bg-surface-container px-2 py-1 rounded">—</span>`;
}

function renderChangeBadgeElement(elementId, change, direction) {
    const el = document.getElementById(elementId);
    if (!el) return;
    el.innerHTML = '';
    if (direction === 'up') {
        el.className = 'flex items-center font-data-mono text-sm px-2 py-1 rounded text-tertiary-container bg-tertiary-container/10';
        el.innerHTML = `<span class="material-symbols-outlined text-sm">trending_up</span>+${change.toFixed(2)}%`;
    } else if (direction === 'down') {
        el.className = 'flex items-center font-data-mono text-sm px-2 py-1 rounded text-error bg-error-container/30';
        el.innerHTML = `<span class="material-symbols-outlined text-sm">trending_down</span>${change.toFixed(2)}%`;
    } else {
        el.className = 'flex items-center font-data-mono text-sm px-2 py-1 rounded text-secondary bg-surface-container';
        el.textContent = '—';
    }
}

// ─── Init ────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadKpis();
    loadLedger();
});

async function refreshAll() {
    clearGoldPricesCache();
    await loadKpis();
    await loadLedger(currentPage);
    toastr.success('تم تحديث البيانات', 'المخزون');
}

// ─── KPIs ───────────────────────────────────

async function loadKpis() {
    const data = await loadGoldPricesKpis();
    if (!data) return;

    // Spot price
    document.getElementById('spotPrice').textContent = data.spotPrice?.displayPrice || '—';
    renderChangeBadgeElement('spotChange', data.spotPrice?.changePercent24H || 0, data.spotPrice?.changeDirection || 'none');

    // 24K per gram
    document.getElementById('k24Price').textContent = data.pricePerGram24K?.displayPrice || '—';
    renderChangeBadgeElement('k24Change', data.pricePerGram24K?.changePercent24H || 0, data.pricePerGram24K?.changeDirection || 'none');

    // 21K per gram
    document.getElementById('k21Price').textContent = data.pricePerGram21K?.displayPrice || '—';
    renderChangeBadgeElement('k21Change', data.pricePerGram21K?.changePercent24H || 0, data.pricePerGram21K?.changeDirection || 'none');

    // Total equivalent 21K
    document.getElementById('totalEquivalent21K').textContent = data.totalEquivalent21KDisplay || '0.000';
    document.getElementById('estimatedValue').textContent = (data.estimatedValueDisplay || '—') + ' د.أ';

    // Karat breakdowns
    renderKaratBreakdowns(data.karatBreakdowns || []);
}

function renderKaratBreakdowns(breakdowns) {
    const container = document.getElementById('karatBreakdowns');
    if (!breakdowns || breakdowns.length === 0) {
        container.innerHTML = '<div class="col-span-2 text-center text-secondary py-8">لا توجد بيانات مخزون</div>';
        return;
    }

    container.innerHTML = breakdowns.map(b => {
        const isPrimary = b.isPrimary;
        const borderClass = isPrimary ? 'border-r-4 border-r-primary-container' : '';
        const badgeClass = isPrimary ? 'bg-primary-container/20 text-on-primary-container' : 'bg-surface-container';
        const weightDisplay = b.totalWeightGrams >= 1000
            ? `${(b.totalWeightGrams / 1000).toFixed(3)} جم`
            : `${b.totalWeightGrams.toFixed(3)} جم`;

        return `
        <div class="bg-surface-container-lowest p-sm rounded-lg border border-outline-variant flex flex-col justify-center ${borderClass}">
            <div class="flex justify-between items-center mb-xs">
                <span class="font-label-md text-secondary">${b.description}</span>
                <span class="px-2 py-1 ${badgeClass} rounded text-xs font-bold">عيار ${b.karat}</span>
            </div>
            <div class="flex items-baseline gap-xs">
                <span class="font-headline-lg text-headline-lg font-data-mono text-on-surface">${b.totalWeightDisplay}</span>
                <span class="text-secondary text-sm">جم</span>
            </div>
        </div>`;
    }).join('');
}

// ─── Ledger Table ────────────────────────────

async function loadLedger(page = 1) {
    currentPage = page;
    const fromDate = document.getElementById('filterFromDate')?.value || '';
    const toDate = document.getElementById('filterToDate')?.value || '';
    const karat = document.getElementById('filterKarat')?.value || '';
    const referenceType = document.getElementById('filterReferenceType')?.value || '';

    const params = new URLSearchParams({
        page: page,
        pageSize: pageSize,
        ...(fromDate && { fromDate }),
        ...(toDate && { toDate }),
        ...(karat && { karat }),
        ...(referenceType && { referenceType })
    });

    try {
        const res = await fetch(`/Inventory/GetLedger?${params}`);
        const data = await res.json();

        const tbody = document.getElementById('ledgerBody');
        if (!data.items || data.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="py-12 text-center text-secondary">لا توجد حركات مخزون</td></tr>';
            document.getElementById('paginationContainer').style.display = 'none';
            return;
        }

        tbody.innerHTML = data.items.map(e => {
            const movementIcon = e.movementIcon;
            const movementColorClass = e.movementColor;

            return `
            <tr class="hover:bg-primary-container/5 transition-colors group">
                <td class="py-3 px-md font-data-mono text-sm text-secondary group-hover:text-primary">${e.id.toString().substring(0, 8).toUpperCase()}</td>
                <td class="py-3 px-md font-data-mono text-sm">${formatDate(e.date)}</td>
                <td class="py-3 px-md">${e.referenceLabel}${e.notes ? ' — ' + e.notes : ''}</td>
                <td class="py-3 px-md"><span class="bg-surface-container px-2 py-1 rounded text-xs">${e.karat}</span></td>
                <td class="py-3 px-md font-data-mono">${e.weightInGrams.toFixed(3)}</td>
                <td class="py-3 px-md font-data-mono text-secondary">${e.equivalent21KWeightInGrams.toFixed(3)}</td>
                <td class="py-3 px-md"><span class="${movementColorClass} font-medium flex items-center gap-1"><span class="material-symbols-outlined text-[16px]">${movementIcon}</span> ${e.movementLabel}</span></td>
                <td class="py-3 px-md"><span class="bg-tertiary-container/20 text-on-tertiary-container px-2 py-1 rounded text-xs">مكتمل</span></td>
            </tr>`;
        }).join('');

        renderPagination(data.totalCount, data.page, data.pageSize);
    } catch (e) {
        console.error('Failed to load ledger', e);
    }
}

function renderPagination(totalCount, page, pageSize) {
    const container = document.getElementById('paginationContainer');
    const info = document.getElementById('paginationInfo');
    const buttons = document.getElementById('paginationButtons');

    const totalPages = Math.ceil(totalCount / pageSize);
    if (totalPages <= 1) {
        container.style.display = 'none';
        return;
    }

    container.style.display = 'flex';
    info.textContent = `${totalCount} حركة — صفحة ${page} من ${totalPages}`;

    let html = '';
    if (page > 1) {
        html += `<button class="px-sm py-1 bg-surface-container border border-outline-variant rounded font-label-md text-label-md hover:bg-surface-container-high transition-colors" onclick="loadLedger(${page - 1})">السابق</button>`;
    }
    if (page < totalPages) {
        html += `<button class="px-sm py-1 bg-primary-container text-on-primary-container rounded font-label-md text-label-md hover:bg-inverse-primary transition-colors" onclick="loadLedger(${page + 1})">التالي</button>`;
    }
    buttons.innerHTML = html;
}