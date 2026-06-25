let purchaseItems = [];
let categories = [];
let accounts = [];
let karatTotals = { 24: 0, 21: 0, 18: 0 };

function formatDate(d) {
    const date = new Date(d);
    return date.toLocaleDateString('ar-JO', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

document.addEventListener('DOMContentLoaded', async () => {
    const today = new Date();
    document.getElementById('invoiceDate').textContent = formatDate(today);
    document.getElementById('invoiceDate').dataset.date = today.toISOString().split('T')[0];

    await loadNextNumber();
    await loadCategories();
    await loadAccounts();
    addItemRow();
    updateAccountSelect();
});

async function loadNextNumber() {
    try {
        const res = await fetch('/CustomerPurchaseInvoices/GetNextNumber');
        const data = await res.json();
        document.getElementById('invoiceNumber').textContent = data.invoiceNumber ?? 'PUR-ERROR';
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
    } catch (e) {
        accounts = [];
    }
}

function addItemRow() {
    purchaseItems.push({ categoryId: null, karat: 21, weight: 0, pricePerGram: 0 });
    renderItems();
    setTimeout(() => {
        const inputs = document.querySelectorAll('#itemsBody input');
        inputs[inputs.length - 1]?.focus({ preventScroll: true });
    }, 50);
}

function removeItemRow(index) {
    purchaseItems.splice(index, 1);
    renderItems();
}

function updateItem(index, field, value) {
    if (field === 'karat') purchaseItems[index].karat = parseInt(value, 10) || 21;
    else if (field === 'weight') purchaseItems[index].weight = parseFloat(value) || 0;
    else if (field === 'pricePerGram') purchaseItems[index].pricePerGram = parseFloat(value) || 0;
    else if (field === 'categoryId') purchaseItems[index].categoryId = value || null;

    updateSummary();
}

function renderItems() {
    const tbody = document.getElementById('itemsBody');

    tbody.innerHTML = purchaseItems.map((item, idx) => {
        const total = (item.weight * item.pricePerGram).toFixed(3);

        return `
            <tr class="border-b border-outline-variant/50 hover:bg-surface-container-lowest/80 transition-colors ${idx % 2 === 1 ? 'bg-surface-container-low/30' : ''}">
                <td class="py-xs px-md">
                    <select class="w-full bg-surface border border-outline-variant rounded-lg px-2 py-2 outline-none text-sm" onchange="updateItem(${idx}, 'categoryId', this.value)">
                        <option value="">اختر الصنف</option>
                        ${categories.map(c => `<option value="${c.id}" ${item.categoryId === c.id ? 'selected' : ''}>${escapeHtml(c.name)}</option>`).join('')}
                    </select>
                </td>
                <td class="py-xs px-sm text-center">
                    <select class="w-full bg-surface border border-outline-variant rounded-lg px-2 py-2 outline-none font-data-mono text-center appearance-none cursor-pointer" onchange="updateItem(${idx}, 'karat', this.value)">
                        <option value="24" ${item.karat === 24 ? 'selected' : ''}>24</option>
                        <option value="21" ${item.karat === 21 ? 'selected' : ''}>21</option>
                        <option value="18" ${item.karat === 18 ? 'selected' : ''}>18</option>
                    </select>
                </td>
                <td class="py-xs px-sm">
                    <div class="relative flex items-center">
                        <input class="w-full bg-surface border border-outline-variant rounded-lg pl-8 pr-2 py-2 outline-none font-data-mono text-right" min="0" type="number" value="${item.weight || ''}" oninput="updateItem(${idx}, 'weight', this.value)" placeholder="0.000" />
                        <span class="absolute left-2 text-secondary font-label-md pointer-events-none">جم</span>
                    </div>
                </td>
                <td class="py-xs px-sm">
                    <input class="w-full bg-surface border border-outline-variant rounded-lg px-2 py-2 outline-none font-data-mono text-right" min="0" type="number" value="${item.pricePerGram || ''}" oninput="updateItem(${idx}, 'pricePerGram', this.value)" placeholder="0.000" />
                </td>
                <td class="py-xs px-md text-left font-data-mono font-bold text-primary" id="itemTotal_${idx}">${total}</td>
                <td class="py-xs px-sm text-center">
                    <button class="text-secondary hover:text-error transition-colors p-1 rounded hover:bg-error-container" onclick="removeItemRow(${idx})">
                        <span class="material-symbols-outlined text-[18px]">delete</span>
                    </button>
                </td>
            </tr>
        `;
    }).join('');

    updateSummary();
}

function updateSummary() {
    const totalAmount = parseFloat(document.getElementById('totalAmount').value) || 0;
    const amountPaid = parseFloat(document.getElementById('amountPaid').value) || 0;
    const remaining = Math.max(0, totalAmount - amountPaid);

    const totalWeight = purchaseItems.reduce((sum, item) => sum + item.weight, 0);
    const itemValue = purchaseItems.reduce((sum, item) => sum + (item.weight * item.pricePerGram), 0);

    document.getElementById('summaryTotalWeight').textContent = totalWeight.toFixed(3);
    document.getElementById('summaryGoldValue').textContent = itemValue.toFixed(3);
    document.getElementById('remainingBalance').textContent = remaining.toFixed(3);
    document.getElementById('summaryCurrency').textContent = getSelectedCurrency() || '---';

    purchaseItems.forEach((item, idx) => {
        const el = document.getElementById(`itemTotal_${idx}`);
        if (el) el.textContent = (item.weight * item.pricePerGram).toFixed(3);
    });

    updateItemsSummary();
}

function updateItemsSummary() {
    const container = document.getElementById('itemsSummary');
    karatTotals = { 24: 0, 21: 0, 18: 0 };

    purchaseItems.forEach(item => {
        if (item.weight > 0) {
            karatTotals[item.karat] = (karatTotals[item.karat] || 0) + item.weight;
        }
    });

    const parts = Object.entries(karatTotals)
        .filter(([, value]) => value > 0)
        .map(([karat, value]) => `
            <div class="flex flex-col">
                <span class="font-label-md text-secondary">إجمالي وزن ${karat}</span>
                <span class="font-data-mono text-body-lg">${value.toFixed(3)} <span class="text-sm font-body-md text-secondary">جم</span></span>
            </div>`);

    container.innerHTML = parts.length === 0
        ? '<span class="font-body-md text-secondary">لم يتم إضافة أصناف بعد</span>'
        : `<div class="flex items-center gap-md flex-wrap">${parts.join('<div class="w-px h-8 bg-outline-variant"></div>')}</div>`;
}

function onCurrencyChange() {
    updateSummary();
    updateAccountSelect();
}

function onPaymentMethodChange() {
    updateAccountSelect();
}

function updateAccountSelect() {
    const currency = getSelectedCurrency();
    const paymentMethod = getSelectedPaymentMethod();
    const accountSelect = document.getElementById('accountSelect');
    const accountHint = document.getElementById('accountHint');
    const referenceWrapper = document.getElementById('referenceWrapper');

    referenceWrapper.classList.toggle('hidden', paymentMethod !== 2);

    if (!currency || !paymentMethod) {
        accountSelect.disabled = true;
        accountSelect.innerHTML = '<option value="">اختر العملة وطريقة الدفع أولاً</option>';
        accountHint.textContent = 'يظهر الحساب بعد اختيار العملة وطريقة الدفع.';
        return;
    }

    const accountType = paymentMethod === 1 ? 'Cash' : 'Bank';
    const filtered = accounts.filter(a => a.currency === currency && a.accountType === accountType);

    accountSelect.disabled = false;
    accountSelect.innerHTML = filtered.length === 0
        ? '<option value="">لا توجد حسابات مطابقة</option>'
        : '<option value="">اختر الحساب</option>' + filtered.map(a => `<option value="${a.id}">${escapeHtml(a.name)} (${a.currency})</option>`).join('');

    if (paymentMethod === 1 && filtered.length > 0) {
        accountSelect.value = filtered[0].id;
        accountHint.textContent = 'تم اختيار حساب النقد المطابق للعملة تلقائياً.';
    } else if (paymentMethod === 2) {
        accountHint.textContent = 'تظهر الحسابات البنكية المطابقة للعملة فقط.';
    } else {
        accountHint.textContent = filtered.length === 0 ? 'لا يوجد حساب مطابق لهذه العملة.' : '';
    }
}

async function submitPurchaseInvoice() {
    const sellerName = document.getElementById('sellerName').value.trim();
    const sellerIdNumber = document.getElementById('sellerIdNumber').value.trim();
    const sellerYearOfBirth = document.getElementById('sellerYearOfBirth').value;
    const sellerPhone = document.getElementById('sellerPhone').value.trim();
    const sellerAddress = document.getElementById('sellerAddress').value.trim();
    const buyerName = document.getElementById('buyerName')?.value || '';
    const dateVal = document.getElementById('invoiceDate').dataset.date || new Date().toISOString();
    const currency = getSelectedCurrency();
    const paymentMethod = getSelectedPaymentMethod();
    const accountId = document.getElementById('accountSelect').value;
    const sellerAccountNumber = document.getElementById('sellerAccountNumber').value.trim();
    const notes = document.getElementById('invoiceNotes').value.trim();
    const totalAmount = parseFloat(document.getElementById('totalAmount').value) || 0;
    const amountPaid = parseFloat(document.getElementById('amountPaid').value) || 0;
    const validItems = purchaseItems.filter(i => i.weight > 0 && i.pricePerGram > 0);

    if (!sellerName) {
        toastr.error('يرجى إدخال اسم البائع');
        return;
    }

    if (!buyerName) {
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

    if (!accountId) {
        toastr.error('يرجى اختيار حساب الدفع');
        return;
    }

    if (validItems.length === 0 || validItems.length !== purchaseItems.length) {
        toastr.error('الوزن والسعر للجرام يجب أن يكونا أكبر من صفر لجميع الأصناف');
        return;
    }

    if (totalAmount <= 0) {
        toastr.error('يرجى إدخال المبلغ المستحق');
        return;
    }

    if (amountPaid < 0 || amountPaid > totalAmount) {
        toastr.error('المبلغ المدفوع يجب أن يكون بين صفر وإجمالي الفاتورة');
        return;
    }

    const payload = {
        sellerName,
        sellerPhone: sellerPhone || null,
        sellerIdNumber,
        sellerYearOfBirth: sellerYearOfBirth ? parseInt(sellerYearOfBirth) : null,
        sellerAddress: sellerAddress || null,
        buyerName,
        date: new Date(dateVal).toISOString(),
        currency,
        items: validItems.map(i => ({
            categoryId: i.categoryId,
            karat: i.karat,
            weightInGrams: i.weight,
            pricePerGram: i.pricePerGram
        })),
        totalAmount,
        amountPaid,
        paymentMethod,
        accountId,
        sellerAccountNumber: sellerAccountNumber || null,
        notes: notes || null
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    try {
        const res = await fetch('/CustomerPurchaseInvoices/Create', {
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
        toastr.error('حدث خطأ في الاتصال', 'مشتريات الذهب');
    }
}

function getSelectedCurrency() {
    return document.querySelector('input[name="currency"]:checked')?.value || null;
}
function getSelectedbuyerName() {
    return document.querySelector('input[name="buyerName"]:checked')?.value || null;
}

function getSelectedPaymentMethod() {
    const value = document.querySelector('input[name="paymentMethod"]:checked')?.value;
    return value ? parseInt(value, 10) : null;
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}

window.refreshCustomerPurchases = function () {
    window.location.reload();
};
