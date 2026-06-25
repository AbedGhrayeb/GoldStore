/* ─── Sales Invoices JS ─────────────────────── */

let items = [];
let categories = [];
let accounts = [];
let currentInvoiceNumber = '';
let karatTotals = { 24: 0, 21: 0, 18: 0 };

function formatDate(d) {
    const date = new Date(d);
    return date.toLocaleDateString('ar-JO', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

// ─── Init ────────────────────────────────────

document.addEventListener('DOMContentLoaded', async () => {
    document.getElementById('invoiceDate').textContent = formatDate(new Date());
    document.getElementById('invoiceDate').dataset.date = new Date().toISOString().split('T')[0];
    await loadNextNumber();
    await loadCategories();
    await loadAccounts();
    addItemRow();
});

async function loadNextNumber() {
    try {
        const res = await fetch('/SalesInvoices/GetNextNumber');
        const data = await res.json();
        currentInvoiceNumber = data.invoiceNumber ?? 'INV-ERROR';
        document.getElementById('invoiceNumber').textContent = currentInvoiceNumber;
    } catch (e) {
        document.getElementById('invoiceNumber').textContent = 'خطأ في التحميل';
    }
}

async function loadCategories() {
    try {
        const res = await fetch('/Categories/GetAll');
        const data = await res.json();
        categories = Array.isArray(data) ? data : (data.items ?? []);
    } catch (e) {
        categories = [];
    }
}

async function loadAccounts() {
    try {
        const res = await fetch('/FinancialAccounts/GetAll');
        const data = await res.json();
        accounts = data.items ?? [];
        onPaymentMethodChange();
    } catch (e) {
        accounts = [];
    }
}

// ─── Items Table ─────────────────────────────

function addItemRow() {
    const index = items.length;
    items.push({ categoryId: null, karat: 21, weight: 0, pricePerGram: 0 });
    renderItems();
    setTimeout(() => {
        const inputs = document.querySelectorAll('#itemsBody input');
        if (inputs.length > 0) inputs[inputs.length - 1]?.focus({ preventScroll: true });
    }, 50);
}

function removeItemRow(index) {
    items.splice(index, 1);
    renderItems();
}

function updateItem(index, field, value) {
    if (field === 'karat') items[index].karat = parseInt(value) || 21;
    else if (field === 'weight') items[index].weight = parseFloat(value) || 0;
    else if (field === 'pricePerGram') items[index].pricePerGram = parseFloat(value) || 0;
    else if (field === 'categoryId') items[index].categoryId = value || null;
    updateSummary();
    updateItemsSummary();
}

function updateItemFromInput(index, field) {
    const el = document.querySelector(`[data-idx="${index}"][data-field="${field}"]`);
    if (!el) return;
    updateItem(index, field, el.value);
}

function renderItems() {
    const tbody = document.getElementById('itemsBody');
    const cats = categories;

    tbody.innerHTML = items.map((item, idx) => {
        const total = (item.weight * item.pricePerGram).toFixed(3);

        return `
            <tr class="border-b border-outline-variant/50 hover:bg-surface-container-lowest/80 transition-colors ${idx % 2 === 1 ? 'bg-surface-container-low/30' : ''}">
                <td class="py-xs px-md">
                    <select data-idx="${idx}" data-field="categoryId" class="w-full bg-surface border border-outline-variant rounded-lg px-2 py-2 outline-none text-sm" onchange="updateItem(${idx}, 'categoryId', this.value)">
                        <option value="">اختر الصنف</option>
                        ${cats.map(c => `<option value="${c.id}" ${item.categoryId === c.id ? 'selected' : ''}>${c.name}</option>`).join('')}
                    </select>
                </td>
                <td class="py-xs px-sm text-center">
                    <select data-idx="${idx}" data-field="karat" class="w-full bg-surface border border-outline-variant rounded-lg px-2 py-2 outline-none font-data-mono text-center appearance-none cursor-pointer" onchange="updateItem(${idx}, 'karat', this.value)">
                        <option value="24" ${item.karat === 24 ? 'selected' : ''}>24</option>
                        <option value="21" ${item.karat === 21 ? 'selected' : ''}>21</option>
                        <option value="18" ${item.karat === 18 ? 'selected' : ''}>18</option>
                    </select>
                </td>
                <td class="py-xs px-sm">
                    <div class="relative flex items-center">
                        <input data-idx="${idx}" data-field="weight" class="w-full bg-surface border border-outline-variant rounded-lg pl-8 pr-2 py-2 outline-none font-data-mono text-right" min="0" type="number" value="${item.weight || ''}" oninput="updateItem(${idx}, 'weight', this.value)" placeholder="0.000" />
                        <span class="absolute left-2 text-secondary font-label-md pointer-events-none">جم</span>
                    </div>
                </td>
                <td class="py-xs px-sm">
                    <div class="relative flex items-center">
                        <input data-idx="${idx}" data-field="pricePerGram" class="w-full bg-surface border border-outline-variant rounded-lg pl-8 pr-2 py-2 outline-none font-data-mono text-right" min="0" type="number" value="${item.pricePerGram || ''}" oninput="updateItem(${idx}, 'pricePerGram', this.value)" placeholder="0.000" />
                        <span class="absolute left-2 text-secondary font-label-md pointer-events-none">د.أ</span>
                    </div>
                </td>
                <td class="py-xs px-md text-left font-data-mono font-bold text-primary" id="itemTotal_${idx}">
                    ${total}
                </td>
                <td class="py-xs px-sm text-center">
                    <button class="text-secondary hover:text-error transition-colors p-1 rounded hover:bg-error-container" onclick="removeItemRow(${idx})">
                        <span class="material-symbols-outlined text-[18px]">delete</span>
                    </button>
                </td>
            </tr>
        `;
    }).join('');
}

function updateItemsSummary() {
    const container = document.getElementById('itemsSummary');
    karatTotals = { 24: 0, 21: 0, 18: 0 };

    items.forEach(item => {
        if (item.weight > 0) {
            karatTotals[item.karat] = (karatTotals[item.karat] || 0) + item.weight;
        }
    });

    const parts = Object.entries(karatTotals)
        .filter(([k, v]) => v > 0)
        .map(([k, v]) =>
            `<div class="flex flex-col">
                <span class="font-label-md text-secondary">إجمالي الوزن ${k}</span>
                <span class="font-data-mono text-body-lg">${v.toFixed(3)} <span class="text-sm font-body-md text-secondary">جم</span></span>
            </div>`
        );

    if (parts.length === 0) {
        container.innerHTML = '<span class="font-body-md text-secondary">لم يتم إضافة أصناف بعد</span>';
    } else {
        container.innerHTML = `<div class="flex items-center gap-md flex-wrap">${parts.join('<div class="w-px h-8 bg-outline-variant"></div>')}</div>`;
    }
}

// ─── Summary Calculations ───────────────────

function updateSummary() {
    const manualTotal = parseFloat(document.getElementById('totalAmount').value) || 0;
    const amountPaid = parseFloat(document.getElementById('amountPaid').value) || 0;
    const remaining = Math.max(0, manualTotal - amountPaid);

    const totalWeight = items.reduce((sum, item) => sum + item.weight, 0);
    document.getElementById('summaryTotalWeight').textContent = totalWeight.toFixed(3);

    const goldValue = items.reduce((sum, item) => sum + (item.weight * item.pricePerGram), 0);
    document.getElementById('summaryGoldValue').textContent = goldValue.toFixed(3);

    document.getElementById('remainingBalance').textContent = remaining.toFixed(3);
    const currency = document.querySelector('input[name="currency"]:checked')?.value || 'JOD';
    document.getElementById('summaryCurrency').textContent = currency;

    items.forEach((item, idx) => {
        const el = document.getElementById(`itemTotal_${idx}`);
        if (el) {
            el.textContent = (item.weight * item.pricePerGram).toFixed(3);
        }
    });

    updateItemsSummary();
}

// ─── Currency / Payment Method ───────────────

function onCurrencyChange() {
    const currency = document.querySelector('input[name="currency"]:checked')?.value || 'JOD';
    document.getElementById('summaryCurrency').textContent = currency;
    const summaryCurrencyMain = document.getElementById('summaryCurrencyMain');
    if (summaryCurrencyMain) summaryCurrencyMain.textContent = currency;
    onPaymentMethodChange();
}

function onPaymentMethodChange() {
    const method = document.querySelector('input[name="paymentMethod"]:checked')?.value;
    const currency = document.querySelector('input[name="currency"]:checked')?.value || 'Jod';
    const bankDetails = document.getElementById('bankDetails');
    const wasHidden = bankDetails.classList.contains('hidden');

    if (method === '2') {
        bankDetails.classList.remove('hidden');
        populateAccounts(currency, 'Bank');
        if (wasHidden) {
            // setTimeout(() => bankDetails.scrollIntoView({ behavior: 'smooth', block: 'nearest' }), 50);
        }
    } else {
        bankDetails.classList.add('hidden');
        populateAccounts(currency, 'Cash');
    }
}

function populateAccounts(currency, type) {
    const sel = document.getElementById('accountSelect');
    const filtered = accounts.filter(a => a.currency === currency && a.accountType === type);
    sel.innerHTML = '<option value="">اختر الحساب</option>' +
        filtered.map(a => `<option value="${a.id}">${a.name} (${a.currency})</option>`).join('');

    // Auto-select if only one
    if (filtered.length === 1) {
        sel.value = filtered[0].id;
    }
}

// ─── Submit ──────────────────────────────────

async function submitInvoice() {
    const customerName = document.getElementById('customerName').value.trim();
    const customerPhone = document.getElementById('customerPhone').value.trim();
    const notes = document.getElementById('invoiceNotes').value.trim();
    const dateVal = document.getElementById('invoiceDate').dataset.date || new Date().toISOString();
    const currency = document.querySelector('input[name="currency"]:checked')?.value;
    const paymentMethod = parseInt(document.querySelector('input[name="paymentMethod"]:checked')?.value) || null;
    const accountId = document.getElementById('accountSelect').value;
    const buyerAccountNumber = document.getElementById('buyerAccountNumber').value.trim();
    const sellerName = document.getElementById('sellerName').value.trim();
    const totalAmount = parseFloat(document.getElementById('totalAmount').value) || 0;
    const amountPaid = parseFloat(document.getElementById('amountPaid').value) || 0;

    // Validate
    if (!customerName) {
        toastr.error('يرجى إدخال اسم العميل');
        return;
    }

    if (!sellerName) {
        toastr.error('يرجى إدخال اسم المشتري');
        return;
    }

    if (!currency) {
        toastr.error('يرجى اختيار عملة الفاتورة');
        return;
    }

    if (!paymentMethod) {
        toastr.error('يرجى اختيار طريقة الدفع');
        return;
    }

    if (totalAmount <= 0) {
        toastr.error('يرجى إدخال المبلغ المستحق');
        return;
    }

    const invalidItems = items.filter(i => i.weight <= 0 || i.pricePerGram <= 0);
    if (invalidItems.length > 0) {
        toastr.error('الوزن والسعر للجرام يجب أن يكونا أكبر من صفر لجميع الأصناف');
        return;
    }

    if (items.length === 0) {
        toastr.error('يرجى إضافة صنف واحد على الأقل');
        return;
    }

    if (paymentMethod === 2) {
        if (!accountId) {
            toastr.error('يرجى اختيار حساب الاستلام');
            return;
        }
        if (!buyerAccountNumber) {
            toastr.error('يرجى إدخال رقم حساب المشتري');
            return;
        }
    }

    const payload = {
        customerName,
        customerPhone: customerPhone || null,
        date: new Date(dateVal).toISOString(),
        currency,
        items: items.map(i => ({
            categoryId: i.categoryId,
            karat: i.karat,
            weightInGrams: i.weight,
            pricePerGram: i.pricePerGram
        })),
        totalAmount,
        amountPaid,
        paymentMethod,
        accountId: accountId || null,
        buyerAccountNumber: buyerAccountNumber || null,
        sellerName: sellerName || null,
        notes: notes || null
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    try {
        const res = await fetch('/SalesInvoices/Create', {
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
        toastr.error('حدث خطأ في الاتصال', 'المبيعات');
    }
}

// ─── Toast callback ─────────────────────────

window.refreshSales = function () {
    // Redirect to sales list or refresh page
    window.location.reload();
};

// ─── Helpers ─────────────────────────────────

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}
