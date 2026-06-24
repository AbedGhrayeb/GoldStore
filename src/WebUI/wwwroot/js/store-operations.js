/* ─── Store Operations JS ──────────────────── */

let currentPage = 1;
const pageSize = 20;

document.addEventListener('DOMContentLoaded', () => {
    loadKpis();
    loadEmployees();
    loadAccounts();
    loadOperations();

    document.getElementById('filterSearch').addEventListener('keydown', (e) => {
        if (e.key === 'Enter') loadOperations(1);
    });
});

// ─── KPIs ───────────────────────────────────

async function loadKpis() {
    try {
        const res = await fetch('/StoreOperations/GetKpis');
        const data = await res.json();
        if (!data) return;

        document.getElementById('kpiSalesCount').textContent = `${data.todaySalesCount} فاتورة`;
        document.getElementById('kpiPurchasesCount').textContent = `${data.todayPurchasesCount} فاتورة`;
        renderCurrencyList('kpiSalesTotals', data.todaySalesTotals);
        renderCurrencyList('kpiPurchasesTotals', data.todayPurchasesTotals);
    } catch (e) {
        console.error('Failed to load KPIs', e);
    }
}

function renderCurrencyList(containerId, items) {
    const container = document.getElementById(containerId);
    if (!items || items.length === 0) {
        container.innerHTML = '<div class="font-data-mono text-data-mono text-secondary">0.000</div>';
        return;
    }
    container.innerHTML = items.map(item =>
        `<div class="font-data-mono text-data-mono text-on-surface kpi-value" dir="ltr">${item.amount.toFixed(3)} ${item.symbol}</div>`
    ).join('');
}

// ─── Filter Dropdowns ───────────────────────

async function loadEmployees() {
    try {
        const res = await fetch('/StoreOperations/GetEmployees');
        const data = await res.json();
        const select = document.getElementById('filterEmployeeName');
        if (Array.isArray(data)) {
            data.forEach(name => {
                select.innerHTML += `<option value="${name}">${name}</option>`;
            });
        }
    } catch (e) {
        console.error('Failed to load employees', e);
    }
}

async function loadAccounts() {
    try {
        const res = await fetch('/FinancialAccounts/GetAll');
        const data = await res.json();
        const select = document.getElementById('filterAccountId');
        if (data && Array.isArray(data.items)) {
            data.items.forEach(a => {
                select.innerHTML += `<option value="${a.id}">${a.name}</option>`;
            });
        }
    } catch (e) {
        console.error('Failed to load accounts', e);
    }
}

// ─── Operations Table ───────────────────────

async function loadOperations(page = 1) {
    currentPage = page;
    const search = document.getElementById('filterSearch')?.value || '';
    const fromDate = document.getElementById('filterFromDate')?.value || '';
    const toDate = document.getElementById('filterToDate')?.value || '';
    const operationType = document.getElementById('filterOperationType')?.value || '';
    const employeeName = document.getElementById('filterEmployeeName')?.value || '';
    const accountId = document.getElementById('filterAccountId')?.value || '';

    const params = new URLSearchParams({
        page: page,
        pageSize: pageSize,
        ...(search && { search }),
        ...(fromDate && { fromDate }),
        ...(toDate && { toDate }),
        ...(operationType && { operationType }),
        ...(employeeName && { employeeName }),
        ...(accountId && { accountId })
    });

    try {
        const res = await fetch(`/StoreOperations/GetPaged?${params}`);
        const data = await res.json();

        const tbody = document.getElementById('operationsBody');
        if (!data.items || data.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="11" class="py-12 text-center text-secondary">لا توجد عمليات</td></tr>';
            document.getElementById('paginationContainer').style.display = 'none';
            document.getElementById('operationsCount').textContent = '';
            return;
        }

        document.getElementById('operationsCount').textContent = `${data.totalCount} عملية`;

        tbody.innerHTML = data.items.map(op => `
            <tr class="border-b border-outline-variant/50 hover:bg-[rgba(212,175,55,0.06)] even:bg-black/[0.02]">
                <td class="px-md py-3">${formatDate(op.date)}</td>
                <td class="px-md py-3 font-data-mono" dir="ltr">${op.invoiceNumber}</td>
                <td class="px-md py-3">${renderOperationBadge(op.operationType)}</td>
                <td class="px-md py-3">${op.counterpartyName}</td>
                <td class="px-md py-3">${op.employeeName ?? '—'}</td>
                <td class="px-md py-3">${op.accountName ?? '—'}</td>
                <td class="px-md py-3 font-data-mono text-left" dir="ltr">${formatAmount(op.totalAmount, op.currencySymbol)}</td>
                <td class="px-md py-3 font-data-mono text-left" dir="ltr">${formatAmount(op.amountPaid, op.currencySymbol)}</td>
                <td class="px-md py-3 font-data-mono text-left" dir="ltr">${formatAmount(op.remainingBalance, op.currencySymbol)}</td>
                <td class="px-md py-3">${renderStatusBadge(op.status, op.statusLabel)}</td>
                <td class="px-md py-3 text-center">
                    <button class="text-primary hover:text-primary-fixed transition-colors" onclick="openDetail('${op.id}','${op.operationType}')" title="عرض">
                        <span class="material-symbols-outlined text-[18px]">visibility</span>
                    </button>
                </td>
            </tr>
        `).join('');

        renderPagination(data.totalCount, data.page, data.pageSize);
    } catch (e) {
        console.error('Failed to load operations', e);
    }
}

function renderOperationBadge(type) {
    if (type === 'Sale') {
        return '<span class="op-badge op-badge-sale"><span class="material-symbols-outlined text-[14px]">sell</span>بيع</span>';
    }
    return '<span class="op-badge op-badge-buy"><span class="material-symbols-outlined text-[14px]">shopping_bag</span>شراء</span>';
}

function renderStatusBadge(status, label) {
    if (!status || !label) return '—';
    const cls = status.toLowerCase();
    return `<span class="status-badge status-badge-${cls}">${label}</span>`;
}

function formatAmount(amount, symbol) {
    return `${Number(amount).toFixed(3)} ${symbol}`;
}

function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString('ar-JO', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

function renderPagination(totalCount, page, size) {
    const container = document.getElementById('paginationContainer');
    const info = document.getElementById('paginationInfo');
    const buttons = document.getElementById('paginationButtons');

    const totalPages = Math.ceil(totalCount / size);
    if (totalPages <= 1) {
        container.style.display = 'none';
        return;
    }

    container.style.display = 'flex';
    info.textContent = `${totalCount} عملية — صفحة ${page} من ${totalPages}`;

    let html = '';
    if (page > 1) {
        html += `<button class="px-sm py-1 bg-surface-container border border-outline-variant rounded font-label-md text-label-md hover:bg-surface-container-high transition-colors" onclick="loadOperations(${page - 1})">السابق</button>`;
    }
    if (page < totalPages) {
        html += `<button class="px-sm py-1 bg-primary-container text-on-primary-container rounded font-label-md text-label-md hover:bg-inverse-primary transition-colors" onclick="loadOperations(${page + 1})">التالي</button>`;
    }
    buttons.innerHTML = html;
}

// ─── Reset Filters ──────────────────────────

function resetFilters() {
    document.getElementById('filterSearch').value = '';
    document.getElementById('filterFromDate').value = '';
    document.getElementById('filterToDate').value = '';
    document.getElementById('filterOperationType').value = '';
    document.getElementById('filterEmployeeName').value = '';
    document.getElementById('filterAccountId').value = '';
    loadOperations(1);
}

// ─── Detail Modal ───────────────────────────

async function openDetail(id, operationType) {
    document.getElementById('detailContent').innerHTML = '<p class="text-center text-secondary py-gutter">جاري التحميل...</p>';
    document.getElementById('detailModal').classList.remove('hidden');

    try {
        const params = new URLSearchParams({ id, operationType });
        const res = await fetch(`/StoreOperations/GetDetail?${params}`);
        const data = await res.json();

        if (!data || data.success === false) {
            document.getElementById('detailContent').innerHTML = '<p class="text-center text-error py-gutter">تعذر تحميل التفاصيل</p>';
            return;
        }

        document.getElementById('detailTitle').textContent = data.invoiceNumber;
        document.getElementById('detailTypeBadge').innerHTML = renderOperationBadge(data.operationType);
        document.getElementById('detailContent').innerHTML = renderDetailContent(data);
    } catch (e) {
        console.error('Failed to load detail', e);
        document.getElementById('detailContent').innerHTML = '<p class="text-center text-error py-gutter">تعذر تحميل التفاصيل</p>';
    }
}

function closeDetail() {
    document.getElementById('detailModal').classList.add('hidden');
}

function renderDetailContent(d) {
    const counterpartyLabel = d.operationType === 'Sale' ? 'العميل' : 'البائع';
    const employeeLabel = d.operationType === 'Sale' ? 'البائع' : 'المشتري';

    let extraFields = '';
    if (d.counterpartyIdNumber) {
        extraFields += `<div><span class="font-label-md text-label-md text-secondary">رقم الهوية</span><p class="font-body-md text-body-md text-on-surface font-data-mono" dir="ltr">${d.counterpartyIdNumber}</p></div>`;
    }
    if (d.counterpartyYearOfBirth) {
        extraFields += `<div><span class="font-label-md text-label-md text-secondary">سنة الميلاد</span><p class="font-body-md text-body-md text-on-surface font-data-mono" dir="ltr">${d.counterpartyYearOfBirth}</p></div>`;
    }
    if (d.counterpartyAddress) {
        extraFields += `<div><span class="font-label-md text-label-md text-secondary">العنوان</span><p class="font-body-md text-body-md text-on-surface">${d.counterpartyAddress}</p></div>`;
    }
    if (d.accountNumber) {
        extraFields += `<div><span class="font-label-md text-label-md text-secondary">رقم الحساب</span><p class="font-body-md text-body-md text-on-surface font-data-mono" dir="ltr">${d.accountNumber}</p></div>`;
    }

    let notesSection = '';
    if (d.notes) {
        notesSection = `
            <div class="mt-md p-sm bg-surface-container-low rounded-lg">
                <span class="font-label-md text-label-md text-secondary">ملاحظات</span>
                <p class="font-body-md text-body-md text-on-surface mt-xs">${d.notes}</p>
            </div>`;
    }

    return `
        <div class="grid grid-cols-2 gap-md mb-md">
            <div><span class="font-label-md text-label-md text-secondary">التاريخ</span><p class="font-body-md text-body-md text-on-surface">${formatDate(d.date)}</p></div>
            <div><span class="font-label-md text-label-md text-secondary">${counterpartyLabel}</span><p class="font-body-md text-body-md text-on-surface">${d.counterpartyName}</p></div>
            <div><span class="font-label-md text-label-md text-secondary">الهاتف</span><p class="font-body-md text-body-md text-on-surface font-data-mono" dir="ltr">${d.counterpartyPhone ?? '—'}</p></div>
            <div><span class="font-label-md text-label-md text-secondary">${employeeLabel}</span><p class="font-body-md text-body-md text-on-surface">${d.employeeName ?? '—'}</p></div>
            <div><span class="font-label-md text-label-md text-secondary">الحساب</span><p class="font-body-md text-body-md text-on-surface">${d.accountName ?? '—'}</p></div>
            <div><span class="font-label-md text-label-md text-secondary">طريقة الدفع</span><p class="font-body-md text-body-md text-on-surface">${d.paymentMethodLabel ?? '—'}</p></div>
            ${extraFields}
        </div>
        <div class="grid grid-cols-3 gap-md mb-md p-sm bg-surface-container-low rounded-lg">
            <div><span class="font-label-md text-label-md text-secondary">الإجمالي</span><p class="font-data-mono text-data-mono text-on-surface" dir="ltr">${formatAmount(d.totalAmount, d.currencySymbol)}</p></div>
            <div><span class="font-label-md text-label-md text-secondary">المدفوع</span><p class="font-data-mono text-data-mono text-on-surface" dir="ltr">${formatAmount(d.amountPaid, d.currencySymbol)}</p></div>
            <div><span class="font-label-md text-label-md text-secondary">المتبقي</span><p class="font-data-mono text-data-mono text-on-surface" dir="ltr">${formatAmount(d.remainingBalance, d.currencySymbol)}</p></div>
        </div>
        ${d.status ? `<div class="mb-md"><span class="font-label-md text-label-md text-secondary">الحالة</span> ${renderStatusBadge(d.status, d.statusLabel)}</div>` : ''}
        <div class="border-t border-outline-variant pt-md">
            <h4 class="font-title-lg text-title-lg text-on-surface mb-sm">الأصناف (${d.items.length})</h4>
            <div class="overflow-x-auto">
                <table class="w-full text-right border-collapse">
                    <thead>
                        <tr class="bg-surface-container">
                            <th class="px-sm py-2 font-label-md text-label-md text-secondary">العيار</th>
                            <th class="px-sm py-2 font-label-md text-label-md text-secondary">الوزن (غ)</th>
                            <th class="px-sm py-2 font-label-md text-label-md text-secondary">معادل 21</th>
                            <th class="px-sm py-2 font-label-md text-label-md text-secondary">سعر الغرام</th>
                            <th class="px-sm py-2 font-label-md text-label-md text-secondary">قيمة الذهب</th>
                            <th class="px-sm py-2 font-label-md text-label-md text-secondary">التصنيف</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${d.items.map(i => `
                            <tr class="border-b border-outline-variant/50">
                                <td class="px-sm py-2 font-data-mono" dir="ltr">${i.karat}K</td>
                                <td class="px-sm py-2 font-data-mono text-left" dir="ltr">${i.weightInGrams.toFixed(3)}</td>
                                <td class="px-sm py-2 font-data-mono text-left" dir="ltr">${i.equivalent21KWeightInGrams.toFixed(3)}</td>
                                <td class="px-sm py-2 font-data-mono text-left" dir="ltr">${i.pricePerGram.toFixed(2)}</td>
                                <td class="px-sm py-2 font-data-mono text-left" dir="ltr">${i.goldAmount.toFixed(2)}</td>
                                <td class="px-sm py-2">${i.categoryName ?? '—'}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        </div>
        ${notesSection}
    `;
}
