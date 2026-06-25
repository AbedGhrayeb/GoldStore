/* ─── Supplier Financial Transactions JS ─── */

let currentPage = 1;
const pageSize = 20;
let allAccounts = [];
let allSuppliers = [];

function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString('ar-JO', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

function currencySymbol(currency) {
    return { Jod: 'د.أ', Usd: '$', Ils: '₪' }[currency] ?? currency;
}

// ─── Init ────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('transactionDate').value = new Date().toISOString().split('T')[0];
    document.getElementById('paymentDate').value = new Date().toISOString().split('T')[0];
    loadAccounts();
    loadSuppliers();
    loadKpis();
    loadTransactions();
});

// ─── Accounts ────────────────────────────────

async function loadAccounts() {
    try {
        const res = await fetch('/SupplierFinancialTransactions/GetAccounts');
        const data = await res.json();
        if (data.success) allAccounts = data.accounts;
    } catch (e) {
        console.error('Failed to load accounts', e);
    }
}

function populateCreateAccountSelect() {
    const currency = document.getElementById('transactionCurrency').value;
    const sel = document.getElementById('transactionAccount');
    const filtered = allAccounts.filter(a => a.currency === currency);
    if (filtered.length === 0) {
        sel.innerHTML = '<option value="">لا توجد حسابات بنفس العملة</option>';
    } else {
        sel.innerHTML = '<option value="">اختر حساب</option>' +
            filtered.map(a => `<option value="${a.id}">${a.name}</option>`).join('');
    }
}

function populatePaymentAccountSelect(currency) {
    const sel = document.getElementById('paymentAccount');
    const filtered = allAccounts.filter(a => a.currency === currency);
    if (filtered.length === 0) {
        sel.innerHTML = '<option value="">لا توجد حسابات بنفس العملة</option>';
    } else {
        sel.innerHTML = '<option value="">اختر حساب</option>' +
            filtered.map(a => `<option value="${a.id}">${a.name}</option>`).join('');
    }
}

// ─── Suppliers ───────────────────────────────

async function loadSuppliers() {
    try {
        const res = await fetch('/Suppliers/List');
        const data = await res.json();
        allSuppliers = Array.isArray(data) ? data : [];
        populateSupplierSelect();
    } catch (e) {
        console.error('Failed to load suppliers', e);
    }
}

function populateSupplierSelect() {
    const sel = document.getElementById('transactionSupplier');
    sel.innerHTML = '<option value="">اختر مورد</option>' +
        allSuppliers.map(s => `<option value="${s.id}">${escapeHtml(s.name)}</option>`).join('');
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

// ─── KPIs ───────────────────────────────────

async function loadKpis() {
    try {
        const res = await fetch('/SupplierFinancialTransactions/GetKpis');
        const data = await res.json();
        if (!data) return;
        renderKpis(data);
    } catch (e) {
        console.error('Failed to load KPIs', e);
    }
}

function renderKpis(kpi) {
    const container = document.getElementById('kpiCards');
    const byCurrency = kpi.byCurrency || [];
    if (byCurrency.length === 0) {
        container.className = 'grid grid-cols-1 md:grid-cols-3 gap-gutter mb-xl';
        container.innerHTML = '<div class="bg-surface-container-lowest rounded-xl border border-outline-variant/30 p-md text-center text-secondary py-8 md:col-span-3">لا توجد بيانات</div>';
        return;
    }

    container.className = 'grid grid-cols-1 md:grid-cols-3 gap-gutter mb-xl';
    container.innerHTML = byCurrency.map(c => `
        <div class="bg-surface-container-lowest rounded-xl shadow-[0px_4px_20px_rgba(0,0,0,0.04)] p-md border border-outline-variant/30">
            <div class="flex items-center justify-between mb-sm">
                <div class="flex items-center gap-xs">
                    <div class="w-9 h-9 rounded-full bg-primary-container/20 flex items-center justify-center text-primary font-bold text-sm">${currencySymbol(c.currency)}</div>
                    <span class="font-title-lg text-title-lg text-on-surface">إجمالي المعاملات (${c.currency.toUpperCase()})</span>
                </div>
                <span class="font-label-md text-label-md text-secondary">${c.transactionCount} معاملة</span>
            </div>
            <div class="space-y-xs">
                <div class="flex items-center justify-between">
                    <span class="font-label-md text-label-md text-secondary">إجمالي له (سلف)</span>
                    <span class="font-data-mono text-data-mono text-primary" dir="ltr">${parseFloat(c.totalFromSupplier).toFixed(3)} ${currencySymbol(c.currency)}</span>
                </div>
                <div class="flex items-center justify-between">
                    <span class="font-label-md text-label-md text-secondary">إجمالي لنا</span>
                    <span class="font-data-mono text-data-mono text-error" dir="ltr">${parseFloat(c.totalToSupplier).toFixed(3)} ${currencySymbol(c.currency)}</span>
                </div>
                <div class="flex items-center justify-between pt-xs border-t border-outline-variant/50">
                    <span class="font-label-md text-label-md text-secondary">الصافي</span>
                    <span class="font-data-mono text-data-mono font-bold ${c.netBalance >= 0 ? 'text-on-surface' : 'text-error'}" dir="ltr">${parseFloat(c.netBalance).toFixed(3)} ${currencySymbol(c.currency)}</span>
                </div>
            </div>
        </div>
    `).join('');
}

// ─── Table ──────────────────────────────────

async function loadTransactions(page = 1) {
    currentPage = page;
    const direction = document.getElementById('filterDirection')?.value || '';
    const search = document.getElementById('filterSearch')?.value || '';

    const params = new URLSearchParams({
        page: page,
        pageSize: pageSize,
        ...(direction && { direction }),
        ...(search && { search })
    });

    try {
        const res = await fetch(`/SupplierFinancialTransactions/GetPaged?${params}`);
        const data = await res.json();

        const tbody = document.getElementById('transactionsBody');
        if (!data.items || data.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="py-12 text-center text-secondary">لا توجد معاملات مالية</td></tr>';
            document.getElementById('paginationContainer').style.display = 'none';
            return;
        }

        tbody.innerHTML = data.items.map(t => {
            const isFromSupplier = t.direction === 'FromSupplier';
            const badgeClass = isFromSupplier ? 'bg-primary-container/20 text-primary' : 'bg-error-container/20 text-error';
            const badgeIcon = isFromSupplier ? 'call_received' : 'call_made';
            const badgeLabel = isFromSupplier ? 'له' : 'لنا';
            const outstanding = t.outstandingBalance ?? t.amount;
            const sym = currencySymbol(t.currency);
            return `
            <tr class="border-b border-outline-variant/50 hover:bg-primary-fixed/5 transition-colors">
                <td class="py-3 px-4 font-medium text-on-surface">${escapeHtml(t.supplierName || '—')}</td>
                <td class="py-3 px-4">
                    <span class="inline-flex items-center gap-1 px-2 py-1 rounded-md text-xs font-bold ${badgeClass}">
                        <span class="material-symbols-outlined text-[14px]">${badgeIcon}</span>
                        ${badgeLabel}
                    </span>
                </td>
                <td class="py-3 px-4 font-data-mono text-left" dir="ltr">${parseFloat(t.amount).toFixed(3)}</td>
                <td class="py-3 px-4 font-data-mono text-left" dir="ltr">${parseFloat(outstanding).toFixed(3)}</td>
                <td class="py-3 px-4 text-secondary">${sym}</td>
                <td class="py-3 px-4 text-on-surface">${formatDate(t.createdAt)}</td>
                <td class="py-3 px-4 text-secondary">${escapeHtml(t.accountName || '—')}</td>
                <td class="py-3 px-4">
                    <button class="p-1.5 rounded hover:bg-surface-container-low text-secondary" onclick="openPaymentModal('${t.id}', ${parseFloat(outstanding).toFixed(3)}, '${t.currency}')" title="تسجيل دفعة">
                        <span class="material-symbols-outlined text-[18px]">payments</span>
                    </button>
                    <button class="p-1.5 rounded hover:bg-surface-container-low text-secondary" onclick="showPayments('${t.id}')" title="عرض الدفعات">
                        <span class="material-symbols-outlined text-[18px]">visibility</span>
                    </button>
                </td>
            </tr>`;
        }).join('');

        renderPagination(data.totalCount, data.page, data.pageSize);
    } catch (e) {
        console.error('Failed to load transactions', e);
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
    info.textContent = `${totalCount} معاملة — صفحة ${page} من ${totalPages}`;

    let html = '';
    if (page > 1) {
        html += `<button class="px-sm py-1 bg-surface-container border border-outline-variant rounded font-label-md text-label-md hover:bg-surface-container-high transition-colors" onclick="loadTransactions(${page - 1})">السابق</button>`;
    }
    if (page < totalPages) {
        html += `<button class="px-sm py-1 bg-primary-container text-on-primary-container rounded font-label-md text-label-md hover:bg-inverse-primary transition-colors" onclick="loadTransactions(${page + 1})">التالي</button>`;
    }
    buttons.innerHTML = html;
}

// ─── Create Transaction ─────────────────────

function openCreateModal() {
    populateSupplierSelect();
    populateCreateAccountSelect();
    document.getElementById('createModal').classList.remove('hidden');
}

function closeCreateModal() {
    document.getElementById('createModal').classList.add('hidden');
}

function updateAmountSuffix() {
    const currency = document.getElementById('transactionCurrency').value;
    document.getElementById('transactionAmountSuffix').textContent = currencySymbol(currency);
}

async function submitTransaction() {
    const supplierId = document.getElementById('transactionSupplier').value;
    const directionRadio = document.querySelector('input[name="direction"]:checked');
    const accountId = document.getElementById('transactionAccount').value;
    const amount = parseFloat(document.getElementById('transactionAmount').value) || 0;
    const currency = document.getElementById('transactionCurrency').value;
    const dateVal = document.getElementById('transactionDate').value;
    const notes = document.getElementById('transactionNotes').value.trim();

    if (!supplierId || !directionRadio || !accountId || !dateVal || amount <= 0) {
        toastr.error('يرجى تعبئة جميع الحقول المطلوبة');
        return;
    }

    const payload = {
        supplierId: supplierId,
        direction: parseInt(directionRadio.value),
        amount: amount,
        currency: currency,
        accountId: accountId,
        date: new Date(dateVal).toISOString(),
        notes: notes || null
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    try {
        const res = await fetch('/SupplierFinancialTransactions/Create', {
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
        toastr.error('حدث خطأ في الاتصال', 'المعاملات المالية');
    }
}

// ─── Payment ─────────────────────────────────

function openPaymentModal(transactionId, outstanding, currency) {
    document.getElementById('paymentTransactionId').value = transactionId;
    document.getElementById('paymentOutstanding').textContent = outstanding.toFixed(3);
    const sym = currencySymbol(currency);
    document.getElementById('paymentCurrencyLabel').textContent = sym;
    document.getElementById('paymentAmountSuffix').textContent = sym;
    document.getElementById('paymentAmount').value = '';
    document.getElementById('paymentNotes').value = '';
    populatePaymentAccountSelect(currency);
    document.getElementById('paymentModal').classList.remove('hidden');
}

function closePaymentModal() {
    document.getElementById('paymentModal').classList.add('hidden');
}

async function submitPayment() {
    const transactionId = document.getElementById('paymentTransactionId').value;
    const accountId = document.getElementById('paymentAccount').value;
    const amount = parseFloat(document.getElementById('paymentAmount').value) || 0;
    const dateVal = document.getElementById('paymentDate').value;
    const notes = document.getElementById('paymentNotes').value.trim();

    if (!transactionId || !accountId || !dateVal || amount <= 0) {
        toastr.error('يرجى تعبئة جميع الحقول المطلوبة');
        return;
    }

    const payload = {
        transactionId: transactionId,
        accountId: accountId,
        amount: amount,
        date: new Date(dateVal).toISOString(),
        notes: notes || null
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    try {
        const res = await fetch('/SupplierFinancialTransactions/CreatePayment', {
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
        toastr.error('حدث خطأ في الاتصال', 'المعاملات المالية');
    }
}

// ─── Toast Callback ─────────────────────────

window.refreshSupplierFinancialTransactions = function () {
    loadKpis();
    loadTransactions(currentPage);
    closeCreateModal();
    closePaymentModal();
};

// ─── View Payments ──────────────────────────

async function showPayments(transactionId) {
    document.getElementById('paymentsList').innerHTML = '<div class="text-center text-secondary py-8">جاري التحميل...</div>';
    document.getElementById('viewPaymentsModal').classList.remove('hidden');

    try {
        const res = await fetch(`/SupplierFinancialTransactions/GetPayments?transactionId=${transactionId}`);
        const data = await res.json();

        const container = document.getElementById('paymentsList');
        if (!data.success || !data.payments || data.payments.length === 0) {
            container.innerHTML = '<div class="text-center text-secondary py-8">لا توجد دفعات مسجلة</div>';
            return;
        }

        container.innerHTML = data.payments.map(p => `
            <div class="flex items-center justify-between p-3 rounded-lg border border-outline-variant/50 bg-surface">
                <div>
                    <div class="font-data-mono text-on-surface" dir="ltr">${parseFloat(p.amount).toFixed(3)}</div>
                    <div class="text-xs text-secondary mt-0.5">${p.accountName ? escapeHtml(p.accountName) : ''}</div>
                    ${p.notes ? `<div class="text-xs text-secondary mt-0.5">${escapeHtml(p.notes)}</div>` : ''}
                </div>
                <div class="text-xs text-secondary">${formatDate(p.date)}</div>
            </div>
        `).join('');
    } catch (e) {
        document.getElementById('paymentsList').innerHTML = '<div class="text-center text-error py-8">حدث خطأ أثناء التحميل</div>';
        console.error('Failed to load payments', e);
    }
}

function closePaymentsModal() {
    document.getElementById('viewPaymentsModal').classList.add('hidden');
}
