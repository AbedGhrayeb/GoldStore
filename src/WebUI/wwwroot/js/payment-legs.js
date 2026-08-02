/* ─── Multi-Currency Payment Legs Editor ────── */

window.PaymentLegs = (function () {
    let config = null;
    let rows = [];

    const CURRENCY_SYMBOLS = { JOD: 'د.أ', USD: '$', ILS: '₪' };

    function round3(value) {
        return Math.round(value * 1000) / 1000;
    }

    function symbol(code) {
        return CURRENCY_SYMBOLS[code] ?? code;
    }

    function accounts() {
        return config.getAccounts ? config.getAccounts() : (config.accounts || []);
    }

    function accountById(id) {
        if (!id) return null;
        return accounts().find(a => a.id === id) || null;
    }

    function baseCurrency() {
        return config.getBaseCurrency ? config.getBaseCurrency() : 'JOD';
    }

    function isSameCurrency(row) {
        return !!row.currency && row.currency === baseCurrency();
    }

    function rateValue(row) {
        if (isSameCurrency(row)) return 1;
        const rate = parseFloat(row.rate);
        return isNaN(rate) ? 0 : rate;
    }

    function equivalentOf(row) {
        const amount = parseFloat(row.amount);
        if (isNaN(amount) || amount <= 0) return 0;
        if (isSameCurrency(row)) return round3(amount);
        return round3(amount * rateValue(row));
    }

    function totalEquivalent() {
        return round3(rows.reduce((sum, row) => sum + equivalentOf(row), 0));
    }

    function capValue() {
        if (!config.getCap) return null;
        const cap = parseFloat(config.getCap());
        return isNaN(cap) ? null : cap;
    }

    function accountOptions(selectedId) {
        const list = accounts();
        if (list.length === 0) {
            return '<option value="">لا توجد حسابات</option>';
        }
        return '<option value="">اختر الحساب</option>' +
            list.map(a => `<option value="${a.id}" ${a.id === selectedId ? 'selected' : ''}>${escapeHtml(a.name)} (${a.currency})</option>`).join('');
    }

    function ratePlaceholder(legCurrency) {
        const base = baseCurrency();
        if (base === 'JOD' && legCurrency === 'USD') return '0.708';
        if (base === 'USD' && legCurrency === 'JOD') return '1.412';
        return '';
    }

    function rowHtml(idx, row) {
        const same = isSameCurrency(row);
        const rateDisplay = same ? '1' : (row.rate || '');
        return `
            <tr class="border-b border-outline-variant/50 hover:bg-surface-container-lowest/80 transition-colors ${idx % 2 === 1 ? 'bg-surface-container-low/30' : ''}">
                <td class="py-xs px-md min-w-[220px]">
                    <select class="w-full bg-surface border border-outline-variant rounded-lg px-2 py-2 outline-none text-sm text-on-surface" onchange="PaymentLegs.onFieldChange(${idx}, 'accountId', this.value)">
                        ${accountOptions(row.accountId)}
                    </select>
                </td>
                <td class="py-xs px-sm text-center">
                    <span class="inline-block font-data-mono text-on-primary-container bg-primary-container/20 rounded px-2 py-1 min-w-[44px]">${row.currency || '—'}</span>
                </td>
                <td class="py-xs px-sm">
                    <input class="w-full bg-surface border border-outline-variant rounded-lg px-2 py-2 outline-none font-data-mono text-right" min="0" step="0.001" type="number" value="${row.amount || ''}" placeholder="0.000" oninput="PaymentLegs.onFieldChange(${idx}, 'amount', this.value)" />
                </td>
                <td class="py-xs px-sm">
                    <input class="w-full bg-surface border border-outline-variant rounded-lg px-2 py-2 outline-none font-data-mono text-right ${same ? 'opacity-50 cursor-not-allowed' : ''}" min="0" step="0.000001" type="number" value="${rateDisplay}" placeholder="${same ? '1' : ratePlaceholder(row.currency)}" ${same ? 'readonly' : ''} oninput="PaymentLegs.onFieldChange(${idx}, 'rate', this.value)" />
                </td>
                <td class="py-xs px-md text-left font-data-mono font-bold text-primary" data-equiv="${idx}">
                    ${equivalentOf(row).toFixed(3)}
                </td>
                <td class="py-xs px-sm text-center">
                    <button class="text-secondary hover:text-error transition-colors p-1 rounded hover:bg-error-container" type="button" onclick="PaymentLegs.removeRow(${idx})">
                        <span class="material-symbols-outlined text-[18px]">delete</span>
                    </button>
                </td>
            </tr>`;
    }

    function totalsHtml() {
        const base = baseCurrency();
        const total = totalEquivalent();
        const cap = capValue();
        const over = cap !== null && total > cap;

        let html = `
            <div class="flex items-center gap-xs">
                <span class="font-label-md text-label-md text-secondary">الإجمالي المكافئ</span>
                <span class="font-data-mono font-bold ${over ? 'text-error' : 'text-on-surface'}" dir="ltr">${total.toFixed(3)} ${symbol(base)}</span>
            </div>`;

        if (cap !== null) {
            const remaining = Math.max(0, cap - total);
            html += `
                <div class="w-px h-6 bg-outline-variant hidden sm:block"></div>
                <div class="flex items-center gap-xs">
                    <span class="font-label-md text-label-md text-secondary">المبلغ المستحق</span>
                    <span class="font-data-mono" dir="ltr">${cap.toFixed(3)} ${symbol(base)}</span>
                </div>
                <div class="flex items-center gap-xs">
                    <span class="font-label-md text-label-md text-secondary">المتبقي</span>
                    <span class="font-data-mono font-bold ${over ? 'text-error' : 'text-primary'}" dir="ltr">${remaining.toFixed(3)} ${symbol(base)}</span>
                </div>`;
        }

        if (over) {
            html += `<span class="font-label-md text-label-md text-error">المبلغ يتجاوز المبلغ المستحق</span>`;
        }

        return `<div class="flex flex-col sm:flex-row sm:items-center gap-xs sm:gap-md flex-wrap">${html}</div>`;
    }

    function renderHeader() {
        const base = baseCurrency();
        const container = document.getElementById(config.containerId);
        container.innerHTML = `
            <div class="bg-surface-container-lowest rounded-xl shadow-[0px_4px_20px_rgba(0,0,0,0.02)] border border-outline-variant/50 p-md flex flex-col gap-sm">
                <div class="flex items-center justify-between">
                    <h3 class="font-title-lg text-title-lg font-bold text-on-surface flex items-center gap-xs">
                        <span class="material-symbols-outlined text-primary">currency_exchange</span>
                        دفعات متعددة
                    </h3>
                    <button class="bg-primary-container text-on-primary-container hover:bg-primary-fixed-dim font-label-md text-label-md px-sm py-xs rounded-lg transition-colors flex items-center gap-xs" type="button" onclick="PaymentLegs.addRow()">
                        <span class="material-symbols-outlined text-[18px]">add</span>
                        إضافة دفعة
                    </button>
                </div>
                <div class="overflow-x-auto">
                    <table class="w-full text-right border-collapse min-w-[640px]">
                        <thead class="bg-surface-container-low text-secondary font-label-md text-label-md">
                            <tr>
                                <th class="py-sm px-md border-b border-outline-variant">الحساب</th>
                                <th class="py-sm px-sm border-b border-outline-variant text-center w-20">العملة</th>
                                <th class="py-sm px-sm border-b border-outline-variant w-32">المبلغ</th>
                                <th class="py-sm px-sm border-b border-outline-variant w-32">سعر الصرف</th>
                                <th class="py-sm px-md border-b border-outline-variant text-left w-32">المكافئ (${symbol(base)})</th>
                                <th class="py-sm px-sm border-b border-outline-variant w-12"></th>
                            </tr>
                        </thead>
                        <tbody id="${config.tbodyId}" class="font-body-md text-on-surface"></tbody>
                    </table>
                </div>
                <div id="${config.totalsId}" class="bg-surface-container-low p-sm rounded-lg border border-outline-variant/50">
                    ${totalsHtml()}
                </div>
                <p class="font-label-md text-label-md text-secondary">المكافئ = المبلغ × سعر الصرف، ويُحتسب بعملة الفاتورة. يُثبت سعر الصرف على 1 تلقائياً عندما تكون العملة نفس عملة الفاتورة.</p>
            </div>`;
    }

    function renderRows() {
        const tbody = document.getElementById(config.tbodyId);
        if (rows.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="py-8 text-center text-secondary">أضف دفعة واحدة على الأقل</td></tr>';
        } else {
            tbody.innerHTML = rows.map((row, idx) => rowHtml(idx, row)).join('');
        }
        renderTotals();
    }

    function renderTotals() {
        const totals = document.getElementById(config.totalsId);
        if (totals) totals.innerHTML = totalsHtml();
        if (config.onTotalsChange) config.onTotalsChange(totalEquivalent());
    }

    function updateRowEquivalent(idx) {
        const cell = document.querySelector(`[data-equiv="${idx}"]`);
        if (cell) cell.textContent = equivalentOf(rows[idx]).toFixed(3);
    }

    return {
        init(cfg) {
            config = cfg;
            rows = [{ accountId: null, currency: null, amount: '', rate: '' }];
            renderHeader();
            renderRows();
        },
        refresh() {
            if (!config) return;
            renderHeader();
            renderRows();
        },
        updateTotals() {
            if (!config) return;
            renderTotals();
        },
        addRow() {
            if (!config) return;
            rows.push({ accountId: null, currency: null, amount: '', rate: '' });
            renderRows();
        },
        removeRow(idx) {
            if (!config || rows.length === 0) return;
            rows.splice(idx, 1);
            renderRows();
        },
        onFieldChange(idx, field, value) {
            if (!config || !rows[idx]) return;
            const row = rows[idx];
            if (field === 'accountId') {
                row.accountId = value || null;
                const acc = accountById(row.accountId);
                row.currency = acc ? acc.currency : null;
                row.rate = '';
                renderRows();
            } else if (field === 'amount') {
                row.amount = value;
                updateRowEquivalent(idx);
                renderTotals();
            } else if (field === 'rate') {
                row.rate = value;
                updateRowEquivalent(idx);
                renderTotals();
            }
        },
        getLegs() {
            return rows.map(row => ({
                accountId: row.accountId,
                currency: row.currency,
                amount: parseFloat(row.amount) || 0,
                exchangeRate: isSameCurrency(row) ? 1 : (parseFloat(row.rate) || 0)
            })).filter(leg => leg.accountId && leg.amount > 0);
        },
        getEquivalentTotal() {
            return totalEquivalent();
        },
        reset() {
            if (!config) return;
            rows = [{ accountId: null, currency: null, amount: '', rate: '' }];
            renderRows();
        },
        validate() {
            if (!config) return 'تعذر تحميل محرر الدفعات';
            if (rows.length === 0) return 'أضف دفعة واحدة على الأقل';

            for (let i = 0; i < rows.length; i++) {
                const row = rows[i];
                if (!row.accountId) return `اختر الحساب للدفعة رقم ${i + 1}`;
                const amount = parseFloat(row.amount);
                if (isNaN(amount) || amount <= 0) return `أدخل مبلغاً أكبر من صفر للدفعة رقم ${i + 1}`;
                if (!isSameCurrency(row)) {
                    const rate = parseFloat(row.rate);
                    if (isNaN(rate) || rate <= 0) return `أدخل سعر صرف أكبر من صفر للدفعة رقم ${i + 1}`;
                }
            }

            const cap = capValue();
            const total = totalEquivalent();
            if (cap !== null && total > cap) {
                return `مجموع الدفعات (${total.toFixed(3)}) يتجاوز المبلغ المستحق (${cap.toFixed(3)}) ${symbol(baseCurrency())}`;
            }

            return null;
        }
    };
})();

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}
