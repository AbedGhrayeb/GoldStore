/* ═══════════════════════════════════════════════════════
   SUPPLIER PAYMENTS — JavaScript
   Handles: supplier selection, balance refresh, gold & manufacturing payments
   ═══════════════════════════════════════════════════════ */

let selectedSupplierId = null;
let financialAccounts = [];
let karatOptions = [];
let currentTransactionTab = 'all';
let lastTransactions = [];
let mfgLegsEnabled = false;
let currentMfgBalance = 0;
let mfgLegsInited = false;

// ── Page Load ────────────────────────────────────────
$(document).ready(function () {
    loadSuppliers();
    loadKarats();
    loadCurrencies();
    loadFinancialAccounts();
});

// ── Load Reference Data ─────────────────────────────
function loadSuppliers() {
    $.ajax({
        url: '/SupplierPayments/GetSuppliers',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            if (Array.isArray(data)) {
                var select = document.getElementById('supplierSelect');
                if (!select) return;
                select.innerHTML = '<option value="">اختر مورد...</option>';
                data.forEach(function (s) {
                    var option = document.createElement('option');
                    option.value = s.id;
                    option.textContent = s.name;
                    select.appendChild(option);
                });
            }
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل الموردين', 'error', 'دفعات المورد');
        }
    });
}

function loadKarats() {
    $.ajax({
        url: '/SupplierPayments/GetKarats',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            karatOptions = data || [];
            var select = document.getElementById('scrapKarat');
            if (!select) return;
            select.innerHTML = '';
            karatOptions.forEach(function (k) {
                var option = document.createElement('option');
                option.value = k.value;
                option.textContent = k.label;
                if (k.value === 21) option.selected = true;
                select.appendChild(option);
            });
        }
    });
}

function loadCurrencies() {
    $.ajax({
        url: '/SupplierPayments/GetCurrencies',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var select = document.getElementById('mfgCurrency');
            if (!select) return;
            select.innerHTML = '';
            data.forEach(function (c) {
                var option = document.createElement('option');
                option.value = c.value;
                option.textContent = c.label;
                if (c.value === 'JOD') option.selected = true;
                select.appendChild(option);
            });
            initMfgLegsEditor();
        }
    });
}

function initMfgLegsEditor() {
    if (mfgLegsInited) return;
    mfgLegsInited = true;
    PaymentLegs.init({
        containerId: 'mfgLegsEditor',
        tbodyId: 'mfgLegsBody',
        totalsId: 'mfgLegsTotals',
        getAccounts: function () {
            return financialAccounts.map(function (a) {
                return { id: a.id, name: a.displayLabel || a.name, currency: a.currency, accountType: a.accountType || 'Cash' };
            });
        },
        getBaseCurrency: function () {
            return document.getElementById('mfgCurrency')?.value || 'JOD';
        },
        getCap: function () {
            return mfgLegsEnabled ? Math.abs(currentMfgBalance) || 0 : null;
        },
        onTotalsChange: function () { }
    });
}

function onMfgLegsToggle(checked) {
    mfgLegsEnabled = checked;
    document.getElementById('singleMfgPaymentFields').classList.toggle('hidden', checked);
    document.getElementById('mfgLegsEditor').classList.toggle('hidden', !checked);
    if (checked) PaymentLegs.refresh();
}

function loadFinancialAccounts() {
    $.ajax({
        url: '/SupplierPayments/GetFinancialAccounts',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            financialAccounts = data || [];
            filterAccountsByCurrency();
            if (window.PaymentLegs && mfgLegsInited) PaymentLegs.refresh();
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل الحسابات المالية', 'error', 'دفعات المورد');
        }
    });
}

function filterAccountsByCurrency() {
    var select = document.getElementById('mfgAccount');
    if (!select) return;

    var currency = document.getElementById('mfgCurrency')?.value || 'JOD';
    var filtered = financialAccounts.filter(function (a) { return a.currency === currency; });

    if (filtered.length === 0) {
        select.innerHTML = '<option value="">لا توجد حسابات بنفس العملة</option>';
    } else {
        select.innerHTML = '<option value="">اختر حساب...</option>';
        filtered.forEach(function (a) {
            var option = document.createElement('option');
            option.value = a.id;
            option.textContent = a.displayLabel + ' (' + a.currency + ')';
            select.appendChild(option);
        });
    }

    if (window.PaymentLegs && mfgLegsEnabled) PaymentLegs.refresh();
}

// ── Supplier Selection ────────────────────────────────
function onSupplierSelected() {
    var select = document.getElementById('supplierSelect');
    selectedSupplierId = select.value;

    if (!selectedSupplierId) {
        document.getElementById('goldBalanceValue').textContent = '—';
        document.getElementById('mfgBalanceValue').textContent = '—';
        document.getElementById('goldBalanceHint').style.display = 'none';
        document.getElementById('mfgBalanceHint').style.display = 'none';
        document.getElementById('btnSubmitScrapGold').disabled = true;
        document.getElementById('btnSubmitManufacturing').disabled = true;
        document.getElementById('btnViewAll').style.display = 'none';
        clearTransactionsTable();
        return;
    }

    document.getElementById('btnSubmitScrapGold').disabled = false;
    document.getElementById('btnSubmitManufacturing').disabled = false;

    refreshSupplierData();
}

function refreshSupplierData() {
    if (!selectedSupplierId) return;

    $.ajax({
        url: '/SupplierPayments/GetSupplierBalances?id=' + encodeURIComponent(selectedSupplierId),
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            if (data.success === false) {
                showToastMessage(data.error || 'حدث خطأ', 'error', 'دفعات المورد');
                return;
            }

            var goldBalance = parseFloat(data.goldBalance) || 0;
            currentMfgBalance = parseFloat(data.manufacturingBalance) || 0;

            document.getElementById('goldBalanceValue').textContent = formatNumber(Math.abs(goldBalance));
            document.getElementById('mfgBalanceValue').textContent = formatNumber(Math.abs(currentMfgBalance));

            updateBalanceHint('gold', goldBalance);
            updateBalanceHint('mfg', currentMfgBalance);

            lastTransactions = data.recentTransactions || [];
            renderTransactions();

            if (window.PaymentLegs && mfgLegsEnabled) PaymentLegs.updateTotals();
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل بيانات المورد', 'error', 'دفعات المورد');
        }
    });
}

function updateBalanceHint(prefix, balance) {
    var hintEl = document.getElementById(prefix + 'BalanceHint');
    var arrowEl = document.getElementById(prefix + 'BalanceArrow');
    var labelEl = document.getElementById(prefix + 'BalanceLabel');

    if (balance === 0) {
        hintEl.style.display = 'none';
        return;
    }

    hintEl.style.display = 'flex';

    if (balance > 0) {
        arrowEl.textContent = 'arrow_downward';
        hintEl.classList.remove('hint-credit');
        hintEl.classList.add('hint-owed');
        labelEl.textContent = 'مطلوب للدفع';
    } else {
        arrowEl.textContent = 'arrow_upward';
        hintEl.classList.remove('hint-owed');
        hintEl.classList.add('hint-credit');
        labelEl.textContent = 'رصيد لنا';
    }
}

// ── Gold Equivalent Calculation (Client Preview) ──────
function calculateEquivalent21K() {
    var weight = parseFloat(document.getElementById('scrapWeight')?.value) || 0;
    var karat = parseInt(document.getElementById('scrapKarat')?.value) || 21;
    var equiv = weight > 0 ? (weight * karat / 21) : 0;
    equiv = Math.round(equiv * 1000) / 1000;

    var equivInput = document.getElementById('scrapEquiv21K');
    if (equivInput) {
        equivInput.value = weight > 0 ? equiv.toFixed(3) : '0.00';
    }
}

// ── Transactions Table ────────────────────────────────
function switchTransactionTab(tab, btn) {
    currentTransactionTab = tab;
    document.querySelectorAll('.tx-tab').forEach(function (t) { t.classList.remove('active'); });
    btn.classList.add('active');
    renderTransactions();
}

function renderTransactions() {
    var tbody = document.getElementById('transactionsBody');
    if (!tbody) return;

    var transactions = lastTransactions;
    if (currentTransactionTab !== 'all') {
        transactions = transactions.filter(function (t) { return t.type === currentTransactionTab; });
    }

    document.getElementById('btnViewAll').style.display = transactions.length > 0 ? 'inline' : 'none';

    if (transactions.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="py-12 text-center text-secondary">لا توجد دفعات سابقة</td></tr>';
        return;
    }

    var html = '';
    transactions.forEach(function (t) {
        var typeIcon = t.type === 'ذهب' ? 'scale' : 'payments';
        var typeLabel = t.type === 'ذهب' ? 'ذهب' : 'أجور';
        var directionClass = t.direction === '+' ? 'direction-increase' : 'direction-decrease';
        var directionLabel = t.direction === '+' ? 'زيادة' : 'نقصان';

        html += '<tr class="hover:bg-primary/5 transition-colors">' +
            '<td class="py-3 px-4 font-body-md text-body-md text-on-surface">' + formatDate(t.date) + '</td>' +
            '<td class="py-3 px-4">' +
                '<span class="type-badge">' +
                    '<span class="material-symbols-outlined" style="font-size:14px">' + typeIcon + '</span> ' + typeLabel +
                '</span>' +
            '</td>' +
            '<td class="py-3 px-4 font-data-mono text-data-mono text-left text-on-surface" dir="ltr">' +
                formatNumber(t.amount) + ' <span class="text-secondary text-xs">' + escapeHtml(t.unit) + '</span>' +
            '</td>' +
            '<td class="py-3 px-4 font-body-md text-body-md text-secondary text-left">' + escapeHtml(t.description) + '</td>' +
            '<td class="py-3 px-4 text-center">' +
                '<span class="' + directionClass + '">' + directionLabel + '</span>' +
            '</td>' +
        '</tr>';
    });

    tbody.innerHTML = html;
}

function clearTransactionsTable() {
    var tbody = document.getElementById('transactionsBody');
    if (tbody) {
        tbody.innerHTML = '<tr><td colspan="5" class="py-12 text-center text-secondary">اختر مورد لعرض الدفعات</td></tr>';
    }
}

// ── Submit Gold Scrap Payment ─────────────────────────
function submitScrapGoldPayment() {
    if (!selectedSupplierId) {
        showToastMessage('يجب اختيار المورد', 'error', 'دفعات المورد');
        return;
    }

    var weight = parseFloat(document.getElementById('scrapWeight')?.value) || 0;
    var karat = parseInt(document.getElementById('scrapKarat')?.value) || 0;
    var notes = document.getElementById('scrapNotes')?.value || '';

    if (weight <= 0) {
        document.getElementById('scrapWeightError').classList.remove('hidden');
        showToastMessage('يجب إدخال الوزن', 'error', 'دفعات المورد');
        return;
    }
    document.getElementById('scrapWeightError').classList.add('hidden');

    var payload = {
        supplierId: selectedSupplierId,
        karat: karat,
        weightInGrams: weight,
        notes: notes || null
    };

    var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    $.ajax({
        url: '/SupplierPayments/CreateScrapGoldPayment',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        headers: { 'RequestVerificationToken': token },
        success: function (json) {
            if (typeof json === 'object' && json.status !== undefined) {
                renderToastFromController(json);
                if (json.status === 1 || json.callback === 'refreshPaymentPage') {
                    refreshPaymentPage();
                }
            }
        },
        error: function (xhr) {
            handleAjaxError(xhr, 'دفعات المورد');
        }
    });
}

// ── Submit Manufacturing Payment ──────────────────────
function submitManufacturingPayment() {
    if (!selectedSupplierId) {
        showToastMessage('يجب اختيار المورد', 'error', 'دفعات المورد');
        return;
    }

    var amount, currency, accountId, notes;
    var paymentLegs = null;

    if (mfgLegsEnabled) {
        var legsError = PaymentLegs.validate();
        if (legsError) {
            showToastMessage(legsError, 'error', 'دفعات المورد');
            return;
        }
        paymentLegs = PaymentLegs.getLegs();
        amount = PaymentLegs.getEquivalentTotal();
        currency = document.getElementById('mfgCurrency')?.value || 'JOD';
        accountId = paymentLegs[0].accountId;
        notes = document.getElementById('mfgNotes')?.value || '';
    } else {
        amount = parseFloat(document.getElementById('mfgAmount')?.value) || 0;
        currency = document.getElementById('mfgCurrency')?.value || 'JOD';
        accountId = document.getElementById('mfgAccount')?.value || '';
        notes = document.getElementById('mfgNotes')?.value || '';

        if (amount <= 0) {
            showToastMessage('يجب إدخال المبلغ', 'error', 'دفعات المورد');
            return;
        }

        if (!accountId) {
            document.getElementById('mfgAccountError').classList.remove('hidden');
            showToastMessage('يجب اختيار حساب الدفع', 'error', 'دفعات المورد');
            return;
        }
        document.getElementById('mfgAccountError').classList.add('hidden');
    }

    var payload = {
        supplierId: selectedSupplierId,
        accountId: accountId,
        amount: amount,
        currency: currency,
        notes: notes || null
    };
    if (paymentLegs) payload.paymentLegs = paymentLegs;

    var token = document.querySelector('#manufacturingForm input[name="__RequestVerificationToken"]')?.value || '';

    $.ajax({
        url: '/SupplierPayments/CreateManufacturingPayment',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        headers: { 'RequestVerificationToken': token },
        success: function (json) {
            if (typeof json === 'object' && json.status !== undefined) {
                renderToastFromController(json);
                if (json.status === 1 || json.callback === 'refreshPaymentPage') {
                    refreshPaymentPage();
                }
            }
        },
        error: function (xhr) {
            handleAjaxError(xhr, 'دفعات المورد');
        }
    });
}

// ── Refresh After Payment ──────────────────────────────
function refreshPaymentPage() {
    refreshSupplierData();

    // Clear forms
    document.getElementById('scrapWeight').value = '';
    document.getElementById('scrapEquiv21K').value = '0.00';
    document.getElementById('scrapNotes').value = '';
    document.getElementById('mfgAmount').value = '';
    document.getElementById('mfgNotes').value = '';
    if (window.PaymentLegs && mfgLegsInited) PaymentLegs.reset();
}

// ── Utility ──────────────────────────────────────────
function formatNumber(num) {
    if (num === null || num === undefined) return '0.00';
    return parseFloat(num).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatDate(dateStr) {
    if (!dateStr) return '—';
    var date = new Date(dateStr);
    var months = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];
    return date.getDate() + ' ' + months[date.getMonth()] + ' ' + date.getFullYear();
}

function escapeHtml(str) {
    if (!str) return '';
    var div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
}

function handleAjaxError(xhr, title) {
    if (xhr.status === 400 && xhr.responseJSON) {
        var errorData = xhr.responseJSON;
        if (errorData.errors) {
            var messages = [];
            for (var key in errorData.errors) {
                if (errorData.errors.hasOwnProperty(key)) {
                    errorData.errors[key].forEach(function (msg) { messages.push(msg); });
                }
            }
            showToastMessage(messages.join(' • '), 'error', title);
        } else if (errorData.title) {
            showToastMessage(errorData.title, 'error', title);
        } else if (errorData.detail) {
            showToastMessage(errorData.detail, 'error', title);
        } else {
            showToastMessage('حدث خطأ في التحقق من البيانات', 'error', title);
        }
    } else {
        showToastMessage('حدث خطأ أثناء الحفظ', 'error', title);
    }
}