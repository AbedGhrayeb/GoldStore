/* ─── Inventory Adjustments JS ─────────────── */

let currentPage = 1;
const pageSize = 20;

function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString('ar-JO', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

// ─── Init ────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('adjDate').value = new Date().toISOString().split('T')[0];
    loadKpis();
    loadAdjustments();
});

// ─── KPIs ───────────────────────────────────

async function loadKpis() {
    try {
        const res = await fetch('/InventoryAdjustments/GetKpis');
        const data = await res.json();
        if (!data) return;

        document.getElementById('kpiTodayCount').textContent = data.todayCount ?? 0;

        const netWeightEl = document.getElementById('kpiNetWeight');
        const trendEl = document.getElementById('kpiNetTrend');
        const containerEl = document.getElementById('kpiNetWeightContainer');

        netWeightEl.textContent = data.netWeightDisplay ?? '0.000';

        if (data.isNegative) {
            containerEl.classList.add('text-error');
            containerEl.classList.remove('text-on-surface');
            trendEl.textContent = 'trending_down';
        } else {
            containerEl.classList.add('text-on-surface');
            containerEl.classList.remove('text-error');
            trendEl.textContent = data.todayCount > 0 ? 'trending_up' : '';
        }
    } catch (e) {
        console.error('Failed to load KPIs', e);
    }
}

// ─── Adjustments Table ──────────────────────

async function loadAdjustments(page = 1) {
    currentPage = page;
    const fromDate = document.getElementById('filterFromDate')?.value || '';
    const toDate = document.getElementById('filterToDate')?.value || '';
    const adjustmentType = document.getElementById('filterType')?.value || '';

    const params = new URLSearchParams({
        page: page,
        pageSize: pageSize,
        ...(fromDate && { fromDate }),
        ...(toDate && { toDate }),
        ...(adjustmentType && { adjustmentType })
    });

    try {
        const res = await fetch(`/InventoryAdjustments/GetPaged?${params}`);
        const data = await res.json();

        const tbody = document.getElementById('adjustmentsBody');
        if (!data.items || data.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="py-12 text-center text-secondary">لا توجد تسويات</td></tr>';
            document.getElementById('paginationContainer').style.display = 'none';
            return;
        }

        tbody.innerHTML = data.items.map(a => `
            <tr class="border-b border-outline-variant/50 hover:bg-primary-fixed/5 transition-colors">
                <td class="py-3 px-4 font-data-mono text-on-surface-variant">${a.id.toString().substring(0, 8).toUpperCase()}</td>
                <td class="py-3 px-4 text-on-surface">${formatDate(a.date)}</td>
                <td class="py-3 px-4">
                    <span class="inline-flex items-center gap-1 px-2 py-1 rounded-md ${a.typeBg} ${a.typeColor} text-xs font-bold">
                        <span class="material-symbols-outlined text-[14px]">${a.typeIcon}</span>
                        ${a.typeLabel}
                    </span>
                </td>
                <td class="py-3 px-4 text-on-surface">عيار ${a.karat}</td>
                <td class="py-3 px-4 font-data-mono text-left" dir="ltr">${a.signedWeight >= 0 ? '+' : ''}${a.signedWeight.toFixed(3)}</td>
                <td class="py-3 px-4 text-secondary truncate max-w-[150px]">${a.reason}</td>
            </tr>
        `).join('');

        renderPagination(data.totalCount, data.page, data.pageSize);
    } catch (e) {
        console.error('Failed to load adjustments', e);
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
    info.textContent = `${totalCount} تسوية — صفحة ${page} من ${totalPages}`;

    let html = '';
    if (page > 1) {
        html += `<button class="px-sm py-1 bg-surface-container border border-outline-variant rounded font-label-md text-label-md hover:bg-surface-container-high transition-colors" onclick="loadAdjustments(${page - 1})">السابق</button>`;
    }
    if (page < totalPages) {
        html += `<button class="px-sm py-1 bg-primary-container text-on-primary-container rounded font-label-md text-label-md hover:bg-inverse-primary transition-colors" onclick="loadAdjustments(${page + 1})">التالي</button>`;
    }
    buttons.innerHTML = html;
}

// ─── Create Adjustment ──────────────────────

async function submitAdjustment() {
    const typeRadio = document.querySelector('input[name="adjustmentType"]:checked');
    const karat = parseInt(document.getElementById('adjKarat').value);
    const weight = parseFloat(document.getElementById('adjWeight').value) || 0;
    const reason = document.getElementById('adjReason').value.trim();
    const notes = document.getElementById('adjNotes').value.trim();
    const dateVal = document.getElementById('adjDate').value;

    if (!typeRadio || !dateVal || !reason) {
        toastr.error('يرجى تعبئة جميع الحقول المطلوبة');
        return;
    }

    const payload = {
        adjustmentType: parseInt(typeRadio.value),
        karat: karat,
        weightInGrams: weight,
        reason: reason,
        notes: notes || null,
        date: new Date(dateVal).toISOString()
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    try {
        const res = await fetch('/InventoryAdjustments/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        });

        const result = await res.json();
        renderToastFromController(result);
    } catch (e) {
        toastr.error('حدث خطأ في الاتصال', 'تسويات المخزون');
    }
}

function resetForm() {
    document.getElementById('adjustmentForm').reset();
    document.getElementById('adjDate').value = new Date().toISOString().split('T')[0];
    document.querySelector('input[name="adjustmentType"][value="1"]').checked = true;
}

// ─── Toast callback ─────────────────────────

window.refreshAdjustments = function () {
    loadAdjustments(currentPage);
    loadKpis();
    resetForm();
};