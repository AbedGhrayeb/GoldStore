/* ═══════════════════════════════════════════════════════
   FINANCIAL ACCOUNTS — JavaScript
   ═══════════════════════════════════════════════════════ */

$(document).ready(function () {
    loadCashAccounts();
    loadBankAccounts();
    loadRecentTransactions();
    loadCurrencies();
});

// ── Cash Accounts ─────────────────────────────────────
function loadCashAccounts() {
    $.ajax({
        url: '/FinancialAccounts/GetCashAccounts',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            if (!Array.isArray(data)) return;
            var container = document.getElementById('cashAccountsContainer');
            if (!container) return;
            container.innerHTML = '';
            data.forEach(function (a) {
                container.innerHTML += buildCashCard(a);
            });
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل الحسابات النقدية', 'error', 'الحسابات المالية');
        }
    });
}

function buildCashCard(a) {
    var changeClass = 'text-secondary';
    var changeIcon = 'horizontal_rule';
    var changeText = 'لا تغيير';

    if (a.lastChangeDirection === 'Inflow' && a.lastChangeAmount > 0) {
        changeClass = 'text-tertiary';
        changeIcon = 'trending_up';
        changeText = '+ ' + formatNumber(a.lastChangeAmount) + ' ' + a.currencySymbol;
    } else if (a.lastChangeDirection === 'Outflow' && a.lastChangeAmount > 0) {
        changeClass = 'text-error';
        changeIcon = 'trending_down';
        changeText = '- ' + formatNumber(a.lastChangeAmount) + ' ' + a.currencySymbol;
    }

    var isPrimary = a.currency === 'JOD';

    return '<div class="account-card bg-surface-container-lowest rounded-xl p-md border border-outline-variant shadow-[0px_4px_20px_rgba(0,0,0,0.04)] relative overflow-hidden group hover:border-primary transition-colors">' +
        (isPrimary ? '<div class="absolute top-0 right-0 w-1 h-full bg-primary-container rounded-r-xl"></div>' : '') +
        '<div class="flex justify-between items-start mb-md">' +
            '<div>' +
                '<p class="font-label-md text-label-md text-secondary uppercase tracking-wider mb-xs">' + getCurrencyLabel(a.currency) + '</p>' +
                '<h4 class="font-headline-md text-headline-md text-on-surface">' + escapeHtml(a.name) + '</h4>' +
            '</div>' +
            '<div class="w-10 h-10 rounded-full bg-surface-container-low flex items-center justify-center ' + (isPrimary ? 'text-primary' : 'text-secondary') + ' border border-outline-variant">' +
                '<span class="font-data-mono text-data-mono">' + a.currency + '</span>' +
            '</div>' +
        '</div>' +
        '<div class="mb-md">' +
            '<span class="font-display-lg text-display-lg text-on-surface" dir="ltr">' + formatNumber(a.balance) + '</span>' +
        '</div>' +
        '<div class="flex items-center justify-between gap-xs">' +
            '<div class="flex items-center gap-xs ' + changeClass + ' font-label-md text-label-md">' +
                '<span class="material-symbols-outlined text-[16px]">' + changeIcon + '</span>' +
                '<span>' + changeText + '</span>' +
            '</div>' +
            buildCardActions(a) +
        '</div>' +
    '</div>';
}

function getCurrencyLabel(currency) {
    switch (currency) {
        case 'JOD': return 'دينار أردني';
        case 'USD': return 'دولار أمريكي';
        case 'ILS': return 'شيكل إسرائيلي';
        default: return currency;
    }
}

// ── Bank Accounts ─────────────────────────────────────
function loadBankAccounts() {
    $.ajax({
        url: '/FinancialAccounts/GetBankAccounts',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            if (!Array.isArray(data)) return;
            var container = document.getElementById('bankAccountsContainer');
            if (!container) return;
            container.innerHTML = '';
            data.forEach(function (a) {
                container.innerHTML += buildBankCard(a);
            });
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل الحسابات البنكية', 'error', 'الحسابات المالية');
        }
    });
}

function buildBankCard(a) {
    var maskedNumber = a.accountNumber ? '**** **** **** ' + a.accountNumber.slice(-4) : '';

    return '<div class="account-card bg-surface-container-lowest/80 backdrop-blur-md rounded-xl p-md border border-outline-variant shadow-[0px_4px_20px_rgba(0,0,0,0.04)] flex flex-col gap-md hover:shadow-[0px_10px_32px_rgba(0,0,0,0.08)] transition-shadow">' +
        '<div class="flex items-start justify-between gap-md">' +
            '<div class="flex items-center gap-md">' +
                '<div class="w-14 h-14 rounded-lg bg-surface-container flex items-center justify-center border border-outline-variant shadow-sm shrink-0">' +
                    '<span class="material-symbols-outlined text-3xl text-primary">corporate_fare</span>' +
                '</div>' +
                '<div>' +
                    '<h4 class="font-headline-md text-headline-md text-on-surface">' + escapeHtml(a.name) + '</h4>' +
                    (maskedNumber ? '<p class="font-body-md text-body-md text-secondary font-data-mono mt-1">' + maskedNumber + '</p>' : '') +
                '</div>' +
            '</div>' +
            '<div class="text-left shrink-0">' +
                '<p class="font-label-md text-label-md text-secondary uppercase mb-xs">الرصيد المتاح (' + a.currency + ')</p>' +
                '<span class="font-headline-lg text-headline-lg text-on-surface font-data-mono" dir="ltr">' + formatNumber(a.balance) + '</span>' +
            '</div>' +
        '</div>' +
        buildCardActions(a) +
    '</div>';
}

// ── Card Action Buttons (shared by cash + bank cards) ──
function buildCardActions(a) {
    return '<div class="flex items-center gap-xs">' +
        '<a href="/FinancialTransactions/Index?accountName=' + encodeURIComponent(a.name) + '" class="inline-flex items-center gap-xs px-sm py-xs bg-surface-container-low hover:bg-surface-container text-on-surface border border-outline-variant rounded-md font-label-md text-label-md transition-colors">' +
            '<span class="material-symbols-outlined text-[16px]">receipt_long</span>' +
            'عرض الحركات' +
        '</a>' +
        '<button type="button" onclick="openSetBalanceModal(\'' + a.id + '\')" class="inline-flex items-center gap-xs px-sm py-xs bg-primary-container/10 hover:bg-primary-container/20 text-primary border border-primary/30 rounded-md font-label-md text-label-md transition-colors">' +
            '<span class="material-symbols-outlined text-[16px]">tune</span>' +
            'ضبط الرصيد' +
        '</button>' +
    '</div>';
}

// ── Recent Transactions ────────────────────────────────
function loadRecentTransactions() {
    $.ajax({
        url: '/FinancialAccounts/GetRecentTransactions',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var tbody = document.getElementById('recentTransactionsBody');
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
                var statusClass = t.transactionType === 'Inflow' ? 'bg-tertiary-container/20 text-tertiary' : 'bg-error-container/20 text-error';

                tbody.innerHTML += '<tr class="border-b border-outline-variant hover:bg-surface-container-low transition-colors">' +
                    '<td class="px-md py-3 font-data-mono text-secondary">' + t.date + '</td>' +
                    '<td class="px-md py-3">' + escapeHtml(t.description) + '</td>' +
                    '<td class="px-md py-3 text-secondary">' + escapeHtml(t.accountName) + '</td>' +
                    '<td class="px-md py-3 font-data-mono ' + amountClass + '" dir="ltr">' + amountPrefix + ' ' + formatNumber(t.amount) + '</td>' +
                    '<td class="px-md py-3 text-center">' +
                        '<span class="inline-flex items-center px-2 py-1 rounded-full text-[10px] font-bold ' + statusClass + '">' + statusLabel + '</span>' +
                    '</td>' +
                '</tr>';
            });
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل الحركات', 'error', 'الحسابات المالية');
        }
    });
}

// ── Create Bank Account Modal ─────────────────────────
function openCreateAccountModal() {
    document.getElementById('createAccountModal').classList.remove('hidden');
}

function closeCreateAccountModal() {
    document.getElementById('createAccountModal').classList.add('hidden');
    resetCreateAccountForm();
}

function resetCreateAccountForm() {
    document.getElementById('accountName').value = '';
    document.getElementById('accountNumber').value = '';
    document.getElementById('accountNotes').value = '';
    var openingInput = document.getElementById('accountOpeningBalance');
    if (openingInput) openingInput.value = '0';
    document.getElementById('accountNameError').classList.add('hidden');
}

function loadCurrencies() {
    $.ajax({
        url: '/FinancialAccounts/GetCurrencies',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var select = document.getElementById('accountCurrency');
            if (!select) return;
            select.innerHTML = '';
            data.forEach(function (c) {
                var option = document.createElement('option');
                option.value = c.value;
                option.textContent = c.label;
                if (c.value === 'JOD') option.selected = true;
                select.appendChild(option);
            });
        }
    });
}

function submitCreateAccount() {
    var name = document.getElementById('accountName').value.trim();
    if (!name) {
        document.getElementById('accountNameError').classList.remove('hidden');
        return;
    }
    document.getElementById('accountNameError').classList.add('hidden');

    var openingRaw = document.getElementById('accountOpeningBalance')?.value || '0';
    var openingBalance = parseFloat(openingRaw) || 0;

    var payload = {
        name: name,
        currency: document.getElementById('accountCurrency').value,
        accountNumber: document.getElementById('accountNumber').value.trim() || null,
        notes: document.getElementById('accountNotes').value.trim() || null,
        openingBalance: openingBalance
    };

    var token = document.querySelector('#createAccountForm input[name="__RequestVerificationToken"]')?.value || '';

    $.ajax({
        url: '/FinancialAccounts/CreateBankAccount',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        headers: { 'RequestVerificationToken': token },
        success: function (json) {
            if (typeof json === 'object' && json.status !== undefined) {
                renderToastFromController(json);
                if (json.status === 1 || json.callback === 'refreshAccountsPage') {
                    closeCreateAccountModal();
                    loadCashAccounts();
                    loadBankAccounts();
                    loadRecentTransactions();
                }
            }
        },
        error: function (xhr) {
            handleAjaxError(xhr, 'الحسابات المالية');
        }
    });
}

// ── Set Balance Modal ─────────────────────────────────
function openSetBalanceModal(accountId) {
    $.ajax({
        url: '/FinancialAccounts/GetAccountBalance',
        type: 'GET',
        data: { id: accountId },
        dataType: 'json',
        success: function (data) {
            if (!data || data.success === false) {
                showToastMessage(data?.error || 'تعذر جلب بيانات الحساب', 'error', 'الحسابات المالية');
                return;
            }
            document.getElementById('setBalanceAccountId').value = data.id;
            document.getElementById('setBalanceAccountName').textContent = data.name + ' (' + data.currency + ')';
            var currentEl = document.getElementById('setBalanceCurrent');
            currentEl.textContent = formatNumber(data.currentBalance) + ' ' + (data.currencySymbol || '');
            document.getElementById('setBalanceTarget').value = formatNumberInput(data.currentBalance);
            document.getElementById('setBalanceNotes').value = '';
            document.getElementById('setBalanceTargetError').classList.add('hidden');
            document.getElementById('setBalanceModal').classList.remove('hidden');
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء جلب بيانات الحساب', 'error', 'الحسابات المالية');
        }
    });
}

function closeSetBalanceModal() {
    document.getElementById('setBalanceModal').classList.add('hidden');
    resetSetBalanceForm();
}

function resetSetBalanceForm() {
    document.getElementById('setBalanceAccountId').value = '';
    document.getElementById('setBalanceAccountName').textContent = '—';
    document.getElementById('setBalanceCurrent').textContent = '0.00';
    document.getElementById('setBalanceTarget').value = '';
    document.getElementById('setBalanceNotes').value = '';
    document.getElementById('setBalanceTargetError').classList.add('hidden');
}

function submitSetBalance() {
    var accountId = document.getElementById('setBalanceAccountId').value;
    if (!accountId) {
        showToastMessage('لم يتم تحديد حساب', 'error', 'الحسابات المالية');
        return;
    }

    var targetRaw = document.getElementById('setBalanceTarget').value;
    var target = parseFloat(targetRaw);
    if (isNaN(target) || target < 0) {
        document.getElementById('setBalanceTargetError').classList.remove('hidden');
        return;
    }
    document.getElementById('setBalanceTargetError').classList.add('hidden');

    var payload = {
        accountId: accountId,
        targetBalance: target,
        notes: document.getElementById('setBalanceNotes').value.trim() || null
    };

    var token = document.querySelector('#setBalanceForm input[name="__RequestVerificationToken"]')?.value || '';

    $.ajax({
        url: '/FinancialAccounts/SetBalanceAjax',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        headers: { 'RequestVerificationToken': token },
        success: function (json) {
            if (typeof json === 'object' && json.status !== undefined) {
                renderToastFromController(json);
                if (json.status === 1 || json.callback === 'refreshAccountsPage') {
                    closeSetBalanceModal();
                    loadCashAccounts();
                    loadBankAccounts();
                    loadRecentTransactions();
                }
            }
        },
        error: function (xhr) {
            handleAjaxError(xhr, 'الحسابات المالية');
        }
    });
}

function refreshAccountsPage() {
    loadCashAccounts();
    loadBankAccounts();
    loadRecentTransactions();
}

function exportReport() {
    showToastMessage('قريباً - تصدير التقرير', 'info', 'الحسابات المالية');
}

// ── Utility ────────────────────────────────────────────
function formatNumber(num) {
    if (num === null || num === undefined) return '0.00';
    return parseFloat(num).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatNumberInput(num) {
    if (num === null || num === undefined) return '0';
    return parseFloat(num).toFixed(2);
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
        } else {
            showToastMessage(errorData.title || errorData.detail || 'حدث خطأ في التحقق من البيانات', 'error', title);
        }
    } else {
        showToastMessage('حدث خطأ أثناء الحفظ', 'error', title);
    }
}
