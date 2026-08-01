/* ─── Debts JS ─────────────────────────────── */

let currentPage = 1;
const pageSize = 20;

function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString('ar-JO', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

function updateAmountSuffix() {
    const currency = document.getElementById('debtCurrency').value;
    const symbols = { JOD: 'د.أ', USD: '$', ILS: '₪' };
    document.getElementById('debtAmountSuffix').textContent = symbols[currency] ?? currency;
}

// ─── Init ────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('debtDate').value = new Date().toISOString().split('T')[0];
    document.getElementById('paymentDate').value = new Date().toISOString().split('T')[0];
    loadKpis();
    loadDebts();
    loadAccounts();
});

// ─── KPIs ───────────────────────────────────

async function loadKpis() {
    try {
        const res = await fetch('/Debts/GetKpis');
        const data = await res.json();
        if (!data) return;

        renderDebtsByCurrency(data.byCurrency);
    } catch (e) {
        console.error('Failed to load KPIs', e);
    }
}

function renderDebtsByCurrency(items) {
    const container = document.getElementById('debtsByCurrencyCards');
    if (!items || items.length === 0) {
        container.className = 'mb-xl';
        container.innerHTML = '<div class="bg-surface-container-lowest rounded-xl border border-outline-variant/30 p-md text-center text-secondary py-8">لا توجد بيانات</div>';
        return;
    }

    container.className = 'grid grid-cols-1 md:grid-cols-3 gap-gutter mb-xl';
    container.innerHTML = items.map(c => `
        <div class="bg-surface-container-lowest rounded-xl shadow-[0px_4px_20px_rgba(0,0,0,0.04)] p-md border border-outline-variant/30">
            <div class="flex items-center justify-between mb-sm">
                <div class="flex items-center gap-xs">
                    <div class="w-9 h-9 rounded-full bg-primary-container/20 flex items-center justify-center text-primary font-bold text-sm">${c.symbol}</div>
                    <span class="font-title-lg text-title-lg text-on-surface">إجمالي الذمم  (${c.currency.toUpperCase()})</span>
                </div>
                <span class="font-label-md text-label-md text-secondary">${c.receivableCount + c.payableCount} دين</span>
            </div>
            <div class="space-y-xs">
                <div class="flex items-center justify-between">
                    <span class="font-label-md text-label-md text-secondary">إجمالي المدينة</span>
                    <span class="font-data-mono text-data-mono text-error" dir="ltr">${c.totalReceivablesDisplay} ${c.symbol}</span>
                </div>
                <div class="flex items-center justify-between">
                    <span class="font-label-md text-label-md text-secondary">إجمالي الدائنة</span>
                    <span class="font-data-mono text-data-mono text-primary" dir="ltr">${c.totalPayablesDisplay} ${c.symbol}</span>
                </div>
                <div class="flex items-center justify-between pt-xs border-t border-outline-variant/50">
                    <span class="font-label-md text-label-md text-secondary">الصافي</span>
                    <span class="font-data-mono text-data-mono font-bold ${c.netBalance >= 0 ? 'text-on-surface' : 'text-error'}" dir="ltr">${c.netBalanceDisplay} ${c.symbol}</span>
                </div>
            </div>
        </div>
    `).join('');
}

// ─── Accounts ────────────────────────────────

let allAccounts = [];

async function loadAccounts() {
    try {
        const res = await fetch('/FinancialAccounts/GetAll');
        const data = await res.json();
        if (!data || !data.items) return;

        allAccounts = data.items;
    } catch (e) {
        console.error('Failed to load accounts', e);
    }
}

function populateCreateAccountSelect() {
    const currency = document.getElementById('debtCurrency').value;
    const sel = document.getElementById('debtAccount');
    const filtered = allAccounts.filter(a => a.currency === currency);
    if (filtered.length === 0) {
        sel.innerHTML = '<option value="">لا توجد حسابات بنفس العملة</option>';
    } else {
        sel.innerHTML = '<option value="">اختر حساب</option>' +
            filtered.map(a => `<option value="${a.id}">${a.name}</option>`).join('');
    }
}

function populateEditAccountSelect(currency, selectedId) {
    const sel = document.getElementById('editDebtAccount');
    const filtered = allAccounts.filter(a => a.currency === currency);
    if (filtered.length === 0) {
        sel.innerHTML = '<option value="">لا توجد حسابات بنفس العملة</option>';
    } else {
        sel.innerHTML = filtered.map(a =>
            `<option value="${a.id}" ${a.id === selectedId ? 'selected' : ''}>${a.name}</option>`
        ).join('');
    }
}

// ─── Debts Table ─────────────────────────────

async function loadDebts(page = 1) {
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
        const res = await fetch(`/Debts/GetPaged?${params}`);
        const data = await res.json();

        const tbody = document.getElementById('debtsBody');
        if (!data.items || data.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="py-12 text-center text-secondary">لا توجد ذمم</td></tr>';
            document.getElementById('paginationContainer').style.display = 'none';
            return;
        }

        tbody.innerHTML = data.items.map(d => `
            <tr class="border-b border-outline-variant/50 hover:bg-primary-fixed/5 transition-colors">
                <td class="py-3 px-4 font-medium text-on-surface">${escapeHtml(d.name)}</td>
                <td class="py-3 px-4 text-secondary">${d.phone ? escapeHtml(d.phone) : '—'}</td>
                <td class="py-3 px-4">
                    <span class="inline-flex items-center gap-1 px-2 py-1 rounded-md text-xs font-bold ${d.direction === 'Receivable' ? 'bg-error-container/20 text-error' : 'bg-primary-container/20 text-primary'}">
                        <span class="material-symbols-outlined text-[14px]">${d.direction === 'Receivable' ? 'account_balance' : 'account_balance_wallet'}</span>
                        ${d.directionLabel}
                    </span>
                </td>
                <td class="py-3 px-4 font-data-mono text-left" dir="ltr">${parseFloat(d.outstandingBalance).toFixed(3)}</td>
                <td class="py-3 px-4 text-secondary">${d.currency === 'JOD' ? 'د.أ' : d.currency}</td>
                <td class="py-3 px-4 text-on-surface">${formatDate(d.createdAt)}</td>
                <td class="py-3 px-4">
                    <div class="flex items-center gap-1">
                        <button class="p-1.5 rounded hover:bg-surface-container-low text-secondary" onclick="openPaymentModal('${d.id}', '${escapeHtml(d.name)}', ${parseFloat(d.outstandingBalance).toFixed(3)}, '${d.currency}')" title="تسديد دفعة">
                            <span class="material-symbols-outlined text-[18px]">payments</span>
                        </button>
                        <button class="p-1.5 rounded hover:bg-surface-container-low text-secondary" onclick="openEditModal('${d.id}', '${escapeHtml(d.name)}', '${escapeHtml(d.phone || '')}', ${parseFloat(d.outstandingBalance).toFixed(3)}, '${d.currency}', '${d.accountId || ''}', '${escapeHtml(d.notes || '')}')" title="تعديل">
                            <span class="material-symbols-outlined text-[18px]">edit</span>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');

        renderPagination(data.totalCount, data.page, data.pageSize);
    } catch (e) {
        console.error('Failed to load debts', e);
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
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
    info.textContent = `${totalCount} دين — صفحة ${page} من ${totalPages}`;

    let html = '';
    if (page > 1) {
        html += `<button class="px-sm py-1 bg-surface-container border border-outline-variant rounded font-label-md text-label-md hover:bg-surface-container-high transition-colors" onclick="loadDebts(${page - 1})">السابق</button>`;
    }
    if (page < totalPages) {
        html += `<button class="px-sm py-1 bg-primary-container text-on-primary-container rounded font-label-md text-label-md hover:bg-inverse-primary transition-colors" onclick="loadDebts(${page + 1})">التالي</button>`;
    }
    buttons.innerHTML = html;
}

// ─── Create Debt ─────────────────────────────

function openCreateModal() {
    populateCreateAccountSelect();
    document.getElementById('createModal').classList.remove('hidden');
}

function closeCreateModal() {
    document.getElementById('createModal').classList.add('hidden');
}

async function submitDebt() {
    const directionRadio = document.querySelector('input[name="direction"]:checked');
    const name = document.getElementById('debtName').value.trim();
    const phone = document.getElementById('debtPhone').value.trim();
    const accountId = document.getElementById('debtAccount').value;
    const amount = parseFloat(document.getElementById('debtAmount').value) || 0;
    const currency = document.getElementById('debtCurrency').value;
    const dateVal = document.getElementById('debtDate').value;
    const notes = document.getElementById('debtNotes').value.trim();

    if (!directionRadio || !name || !accountId || !dateVal || amount <= 0) {
        toastr.error('يرجى تعبئة جميع الحقول المطلوبة');
        return;
    }

    const payload = {
        name: name,
        phone: phone || null,
        direction: parseInt(directionRadio.value),
        currency: currency,
        accountId: accountId,
        amount: amount,
        notes: notes || null,
        date: new Date(dateVal).toISOString()
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    try {
        const res = await fetch('/Debts/Create', {
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
        toastr.error('حدث خطأ في الاتصال', 'الذمم');
    }
}

// ─── Payment ─────────────────────────────────

function openPaymentModal(id, name, balance, currency) {
    const symbols = { JOD: 'د.أ', USD: '$', ILS: '₪' };
    const symbol = symbols[currency] ?? currency;

    document.getElementById('paymentDebtId').value = id;
    document.getElementById('paymentDebtName').textContent = name;
    document.getElementById('paymentRemainingBalance').textContent = balance.toFixed(3);
    document.getElementById('paymentCurrencySymbol').textContent = symbol;
    document.getElementById('paymentAmountSuffix').textContent = symbol;
    document.getElementById('paymentAmount').value = '';
    document.getElementById('paymentNotes').value = '';

    const sel = document.getElementById('paymentAccount');
    const filtered = allAccounts.filter(a => a.currency === currency);
    if (filtered.length === 0) {
        sel.innerHTML = '<option value="">لا توجد حسابات بنفس العملة</option>';
    } else {
        sel.innerHTML = '<option value="">اختر حساب</option>' +
            filtered.map(a => `<option value="${a.id}">${a.name}</option>`).join('');
    }

    document.getElementById('paymentModal').classList.remove('hidden');
}

function closePaymentModal() {
    document.getElementById('paymentModal').classList.add('hidden');
}

async function submitPayment() {
    const debtId = document.getElementById('paymentDebtId').value;
    const accountId = document.getElementById('paymentAccount').value;
    const amount = parseFloat(document.getElementById('paymentAmount').value) || 0;
    const dateVal = document.getElementById('paymentDate').value;
    const notes = document.getElementById('paymentNotes').value.trim();

    if (!debtId || !accountId || !dateVal || amount <= 0) {
        toastr.error('يرجى تعبئة جميع الحقول المطلوبة');
        return;
    }

    const payload = {
        debtId: debtId,
        accountId: accountId,
        amount: amount,
        date: new Date(dateVal).toISOString(),
        notes: notes || null
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    try {
        const res = await fetch('/Debts/CreatePayment', {
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
        toastr.error('حدث خطأ في الاتصال', 'الذمم');
    }
}

// ─── Edit Debt ───────────────────────────────

function openEditModal(id, name, phone, amount, currency, accountId, notes) {
    document.getElementById('editDebtId').value = id;
    document.getElementById('editDebtName').value = name;
    document.getElementById('editDebtPhone').value = phone;
    document.getElementById('editDebtAmount').value = amount;
    document.getElementById('editDebtNotes').value = notes;

    const symbols = { JOD: 'د.أ', USD: '$', ILS: '₪' };
    document.getElementById('editDebtAmountSuffix').textContent = symbols[currency] ?? currency;

    populateEditAccountSelect(currency, accountId);

    document.getElementById('editModal').classList.remove('hidden');
}

function closeEditModal() {
    document.getElementById('editModal').classList.add('hidden');
}

async function submitEdit() {
    const id = document.getElementById('editDebtId').value;
    const name = document.getElementById('editDebtName').value.trim();
    const phone = document.getElementById('editDebtPhone').value.trim();
    const amount = parseFloat(document.getElementById('editDebtAmount').value) || null;
    const accountId = document.getElementById('editDebtAccount').value;
    const notes = document.getElementById('editDebtNotes').value.trim();

    if (!id) {
        toastr.error('معرف الدين مطلوب');
        return;
    }

    const payload = {
        id: id,
        name: name || null,
        phone: phone || null,
        newAmount: amount,
        newAccountId: accountId || null,
        notes: notes || null
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    try {
        const res = await fetch('/Debts/Update', {
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
        toastr.error('حدث خطأ في الاتصال', 'الذمم');
    }
}

// ─── Toast callback ─────────────────────────

window.refreshDebts = function () {
    loadKpis();
    loadDebts(currentPage);
    closeCreateModal();
    closePaymentModal();
    closeEditModal();
};

window.refreshDebtsAndAccounts = function () {
    refreshDebts();
    if (typeof refreshAccountsPage === 'function') refreshAccountsPage();
};