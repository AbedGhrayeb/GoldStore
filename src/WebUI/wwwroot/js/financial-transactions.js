/* ═══════════════════════════════════════════════════════
   FINANCIAL TRANSACTIONS — JavaScript
   Paged, filtered transaction history
   ═══════════════════════════════════════════════════════ */

var currentPage = 1;
var totalPages = 1;

$(document).ready(function () {
    loadFilterCurrencies();

    var urlParams = new URLSearchParams(window.location.search);
    var prefillAccount = urlParams.get('accountName');
    if (prefillAccount) {
        document.getElementById('filterAccountName').value = prefillAccount;
    }

    loadTransactions(1);
});

function loadFilterCurrencies() {
    $.ajax({
        url: '/FinancialTransactions/GetCurrencies',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var select = document.getElementById('filterCurrency');
            if (!select) return;
            select.innerHTML = '<option value="">الكل</option>';
            data.forEach(function (c) {
                var option = document.createElement('option');
                option.value = c.value;
                option.textContent = c.label;
                select.appendChild(option);
            });
        }
    });
}

function loadTransactions(page) {
    currentPage = page;

    var params = {
        page: page,
        pageSize: 20,
        accountName: document.getElementById('filterAccountName')?.value.trim() || null,
        fromDate: document.getElementById('filterFromDate')?.value || null,
        toDate: document.getElementById('filterToDate')?.value || null,
        currency: document.getElementById('filterCurrency')?.value || null,
        accountType: document.getElementById('filterAccountType')?.value || null
    };

    $.ajax({
        url: '/FinancialTransactions/GetPagedTransactions',
        type: 'GET',
        data: params,
        dataType: 'json',
        success: function (data) {
            if (!data || !data.success) {
                var tbody = document.getElementById('allTransactionsBody');
                if (tbody) tbody.innerHTML = '<tr><td colspan="5" class="py-12 text-center text-secondary">حدث خطأ أثناء التحميل</td></tr>';
                return;
            }

            renderTransactions(data.items || []);
            renderPagination(data.totalCount, data.page, data.pageSize, data.totalPages);
        },
        error: function () {
            var tbody = document.getElementById('allTransactionsBody');
            if (tbody) tbody.innerHTML = '<tr><td colspan="5" class="py-12 text-center text-secondary">حدث خطأ أثناء التحميل</td></tr>';
        }
    });
}

function renderTransactions(items) {
    var tbody = document.getElementById('allTransactionsBody');
    if (!tbody) return;

    if (items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="py-12 text-center text-secondary">لا توجد حركات مطابقة للبحث</td></tr>';
        return;
    }

    tbody.innerHTML = '';
    items.forEach(function (t) {
        var amountClass = t.transactionType === 'Inflow' ? 'text-tertiary' : 'text-error';
        var amountPrefix = t.transactionType === 'Inflow' ? '+' : '-';
        var directionLabel = t.transactionType === 'Inflow' ? 'إيداع' : 'سحب';
        var directionClass = t.transactionType === 'Inflow' ? 'bg-tertiary-container/20 text-tertiary' : 'bg-error-container/20 text-error';

        tbody.innerHTML += '<tr class="border-b border-outline-variant hover:bg-surface-container-low transition-colors">' +
            '<td class="px-md py-3 font-data-mono text-secondary">' + t.date + '</td>' +
            '<td class="px-md py-3">' + escapeHtml(t.description) + '</td>' +
            '<td class="px-md py-3 text-secondary">' + escapeHtml(t.accountName) + '</td>' +
            '<td class="px-md py-3 font-data-mono ' + amountClass + '" dir="ltr">' + amountPrefix + ' ' + formatNumber(t.amount) + ' <span class="text-secondary text-xs">' + escapeHtml(t.currencySymbol) + '</span></td>' +
            '<td class="px-md py-3 text-center">' +
                '<span class="inline-flex items-center px-2 py-1 rounded-full text-[10px] font-bold ' + directionClass + '">' + directionLabel + '</span>' +
            '</td>' +
        '</tr>';
    });
}

function renderPagination(totalCount, page, pageSize, tp) {
    totalPages = tp;
    var container = document.getElementById('paginationContainer');
    if (!container) return;

    if (totalCount === 0) {
        container.style.display = 'none';
        return;
    }

    container.style.display = 'flex';
    document.getElementById('paginationInfo').textContent = 'عرض ' + ((page - 1) * pageSize + 1) + '-' + Math.min(page * pageSize, totalCount) + ' من ' + totalCount;
    document.getElementById('pageIndicator').textContent = page + ' / ' + totalPages;
    document.getElementById('btnPrevPage').disabled = page <= 1;
    document.getElementById('btnNextPage').disabled = page >= totalPages;
}

function goToPage(page) {
    if (page < 1 || page > totalPages) return;
    loadTransactions(page);
}

function applyFilters() {
    loadTransactions(1);
}

function resetFilters() {
    document.getElementById('filterAccountName').value = '';
    document.getElementById('filterFromDate').value = '';
    document.getElementById('filterToDate').value = '';
    document.getElementById('filterCurrency').value = '';
    document.getElementById('filterAccountType').value = '';
    loadTransactions(1);
}

function formatNumber(num) {
    if (num === null || num === undefined) return '0.00';
    return parseFloat(num).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function escapeHtml(str) {
    if (!str) return '';
    var div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
}