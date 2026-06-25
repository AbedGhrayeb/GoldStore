/* ═══════════════════════════════════════════════════════
   SUPPLIERS PAGE — JavaScript
   BureauVeritas pattern: loadDataFromUrl, openModal, callback refresh
   ═══════════════════════════════════════════════════════ */

let allSuppliers = [];
let selectedSupplierId = null;
let currentFilter = 'all';
let currentSearch = '';

// ── Load Supplier Table from JSON ────────────────────
function loadSupplierTable() {
    $.ajax({
        url: '/Suppliers/List',
        type: 'GET',
        dataType: 'json',
        contentType: 'application/json',
        success: function (data) {
            allSuppliers = Array.isArray(data) ? data : [];
            renderTable();
            updateCounts();
            autoSelectFirst();
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل الموردين', 'error', 'إدارة الموردين');
            const tbody = document.getElementById('suppliersTableBody');
            const cards = document.getElementById('suppliersCards');
            if (tbody) tbody.innerHTML = '<tr><td colspan="5" class="text-center py-12 text-red-500">فشل تحميل البيانات. <a href="#" onclick="loadSupplierTable(); return false;" class="text-gold underline">إعادة المحاولة</a></td></tr>';
            if (cards) cards.innerHTML = '<div class="text-center py-12 text-red-500">فشل تحميل البيانات</div>';
        }
    });
}

// ── Refresh (callback from ToastResult) ──────────────
function refreshSupplierTable() {
    $.ajax({
        url: '/Suppliers/List',
        type: 'GET',
        dataType: 'json',
        contentType: 'application/json',
        success: function (data) {
            allSuppliers = Array.isArray(data) ? data : [];
            renderTable();
            updateCounts();
            reapplyFilter();
            if (selectedSupplierId) {
                selectSupplier(selectedSupplierId);
            }
        }
    });
}

function renderTable() {
    const tbody = document.getElementById('suppliersTableBody');
    const cardsContainer = document.getElementById('suppliersCards');
    const emptyDesktop = document.getElementById('emptyStateDesktop');
    const emptyMobile = document.getElementById('emptyStateMobile');

    if (allSuppliers.length === 0) {
        tbody.innerHTML = '';
        cardsContainer.innerHTML = '';
        if (emptyDesktop) emptyDesktop.classList.remove('hidden');
        if (emptyMobile) emptyMobile.classList.remove('hidden');
        return;
    }

    if (emptyDesktop) emptyDesktop.classList.add('hidden');
    if (emptyMobile) emptyMobile.classList.add('hidden');

    tbody.innerHTML = allSuppliers.map(s => renderDesktopRow(s)).join('');
    cardsContainer.innerHTML = allSuppliers.map(s => renderMobileCard(s)).join('');
}

function renderDesktopRow(s) {
    const isSelected = s.id === selectedSupplierId;
    const statusClass = s.isActive ? 'status-active' : 'status-stopped';
    const statusText = s.isActive ? 'نشط' : 'متوقف';
    const goldClass = s.goldBalance < 0 ? 'text-error' : 'text-on-surface';
    const initial = s.name ? s.name.charAt(0) : '?';

    return `<tr class="supplier-row hover:bg-primary-fixed/10 transition-colors cursor-pointer${isSelected ? ' selected' : ''}"
                data-supplier-id="${s.id}" data-is-active="${s.isActive}" data-search-text="${s.name} ${s.primaryPhone}"
                onclick="selectSupplier('${s.id}')">
            <td class="py-3 px-4">
                <div class="flex items-center gap-3">
                    <div class="supplier-avatar">${initial}</div>
                    <div class="text-on-surface font-semibold">${escapeHtml(s.name)}</div>
                </div>
            </td>
            <td class="py-3 px-4 data-mono ${goldClass}" dir="ltr">${formatNumber(s.goldBalance)}</td>
            <td class="py-3 px-4 data-mono text-on-surface" dir="ltr">${formatNumber(s.manufacturingBalance)}</td>
            <td class="py-3 px-4"><span class="${statusClass}">${statusText}</span></td>
            <td class="py-3 px-4">
                <div class="flex items-center gap-1">
                    <a href="/Suppliers/AddOrUpdate/${s.id}" ajaxType="GET" title="تعديل"
                       class="openModal p-1.5 text-gray-400 hover:text-gold hover:bg-gold/5 rounded-lg transition-colors">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                    </a>
                    <button onclick="event.stopPropagation(); toggleActive('${s.id}')"
                            class="p-1.5 ${s.isActive ? 'text-green-500 hover:text-red-500 hover:bg-red-50' : 'text-red-400 hover:text-green-500 hover:bg-green-50'} rounded-lg transition-colors"
                            title="${s.isActive ? 'إلغاء التفعيل' : 'تفعيل'}">
                        ${s.isActive
                            ? '<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"/></svg>'
                            : '<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>'}
                    </button>
                </div>
            </td>
        </tr>`;
}

function renderMobileCard(s) {
    const statusClass = s.isActive ? 'status-active' : 'status-stopped';
    const statusText = s.isActive ? 'نشط' : 'متوقف';
    const goldClass = s.goldBalance < 0 ? 'text-error' : 'text-on-surface';
    const initial = s.name ? s.name.charAt(0) : '?';

    return `<div class="supplier-mobile-card"
                 data-supplier-id="${s.id}" data-is-active="${s.isActive}" data-search-text="${s.name} ${s.primaryPhone}"
                 onclick="selectSupplier('${s.id}')">
            <div class="flex items-center justify-between">
                <div class="flex items-center gap-3">
                    <div class="supplier-avatar">${initial}</div>
                    <div>
                        <div class="text-on-surface font-semibold">${escapeHtml(s.name)}</div>
                        <div class="text-secondary text-xs" dir="ltr">${escapeHtml(s.primaryPhone)}</div>
                    </div>
                </div>
                <div class="flex items-center gap-2">
                    <a href="/Suppliers/AddOrUpdate/${s.id}" ajaxType="GET" title="تعديل"
                       class="openModal p-2 text-gray-400 hover:text-gold hover:bg-gold/5 rounded-lg transition-colors" onclick="event.stopPropagation();">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                    </a>
                    <span class="${statusClass}">${statusText}</span>
                </div>
            </div>
            <div class="mt-3 flex gap-4">
                <div>
                    <div class="text-secondary text-xs">رصيد الذهب</div>
                    <div class="data-mono ${goldClass}" dir="ltr">${formatNumber(s.goldBalance)} جم</div>
                </div>
                <div>
                    <div class="text-secondary text-xs">أجور التصنيع</div>
                    <div class="data-mono text-on-surface" dir="ltr">${formatNumber(s.manufacturingBalance)} د.إ</div>
                </div>
            </div>
        </div>`;
}

// ── Filter Tabs ──────────────────────────────────────
function updateCounts() {
    const all = allSuppliers.length;
    const active = allSuppliers.filter(s => s.isActive).length;
    const stopped = all - active;
    const countAll = document.getElementById('countAll');
    const countActive = document.getElementById('countActive');
    const countStopped = document.getElementById('countStopped');
    if (countAll) countAll.textContent = all;
    if (countActive) countActive.textContent = active;
    if (countStopped) countStopped.textContent = stopped;
}

function filterSuppliers(status) {
    currentFilter = status;
    const tabs = document.querySelectorAll('.filter-tab');
    tabs.forEach(t => t.classList.remove('active'));
    if (event && event.currentTarget) event.currentTarget.classList.add('active');
    reapplyFilter();
}

function reapplyFilter() {
    searchSuppliers();
}

function searchSuppliers() {
    currentSearch = document.getElementById('supplierSearch')?.value.trim().toLowerCase() || '';
    const rows = document.querySelectorAll('.supplier-row');
    const cards = document.querySelectorAll('.supplier-mobile-card');
    let visibleCount = 0;

    rows.forEach(row => {
        const text = (row.getAttribute('data-search-text') || '').toLowerCase();
        const isActive = row.getAttribute('data-is-active') === 'true';
        const matchFilter = currentFilter === 'all' ||
            (currentFilter === 'active' && isActive) ||
            (currentFilter === 'stopped' && !isActive);
        const matchSearch = !currentSearch || text.includes(currentSearch);
        const show = matchFilter && matchSearch;
        row.style.display = show ? '' : 'none';
        if (show) visibleCount++;
    });

    cards.forEach(card => {
        const text = (card.getAttribute('data-search-text') || '').toLowerCase();
        const isActive = card.getAttribute('data-is-active') === 'true';
        const matchFilter = currentFilter === 'all' ||
            (currentFilter === 'active' && isActive) ||
            (currentFilter === 'stopped' && !isActive);
        const matchSearch = !currentSearch || text.includes(currentSearch);
        const show = matchFilter && matchSearch;
        card.style.display = show ? '' : 'none';
    });

    const noResults = document.getElementById('noResults');
    if (visibleCount === 0 && (currentSearch || currentFilter !== 'all')) {
        noResults?.classList.remove('hidden');
    } else {
        noResults?.classList.add('hidden');
    }
}

// ── Select Supplier (Master-Detail) ──────────────────
function selectSupplier(id) {
    selectedSupplierId = id;
    document.querySelectorAll('.supplier-row').forEach(r => r.classList.remove('selected'));
    document.querySelectorAll('.supplier-mobile-card').forEach(c => c.classList.remove('selected'));
    const row = document.querySelector(`[data-supplier-id="${id}"]`);
    if (row) row.classList.add('selected');

    if (window.innerWidth >= 1024) {
        loadSupplierDetail(id);
    } else {
        openMobileDetail(id);
    }
}

async function loadSupplierDetail(id) {
    const content = document.getElementById('detailContent');
    content.innerHTML = '<div class="flex items-center justify-center h-40"><div class="text-secondary">جاري التحميل...</div></div>';

    try {
        const response = await fetch(`/Suppliers/GetById?id=${id}`);
        const data = await response.json();
        if (!data.success) { content.innerHTML = `<div class="text-error p-4">حدث خطأ: ${escapeHtml(data.error)}</div>`; return; }
        renderDetailPanel(content, data);
    } catch (e) {
        content.innerHTML = '<div class="text-error p-4">حدث خطأ أثناء التحميل</div>';
    }
}

function renderDetailPanel(container, data) {
    const goldBalance = data.goldBalance;
    const mfgBalance = data.manufacturingBalance;
    const goldDisplay = formatNumber(Math.abs(goldBalance));
    const mfgDisplay = formatNumber(Math.abs(mfgBalance));
    const goldHint = balanceHintHtml(goldBalance);
    const mfgHint = balanceHintHtml(mfgBalance);
    let finHtml = '';
    if (data.financialBalancesByCurrency && data.financialBalancesByCurrency.length > 0) {
        finHtml = data.financialBalancesByCurrency.map(c => {
            const cls = c.balance < 0 ? 'text-error' : c.balance > 0 ? 'text-success' : 'text-on-surface';
            const sign = c.balance > 0 ? 'له' : c.balance < 0 ? 'لنا' : '';
            return `<div class="flex items-center justify-between py-0.5">
                <span class="text-xs text-secondary">${escapeHtml(c.currency)}</span>
                <span class="data-mono text-sm font-medium ${cls}" dir="ltr">${formatNumber(Math.abs(c.balance))} <span class="text-xs">${sign}</span></span>
            </div>`;
        }).join('');
    } else {
        finHtml = '<div class="text-secondary text-xs text-center py-2">لا توجد معاملات مالية</div>';
    }
    let transactionsHtml = '';
    if (data.recentTransactions && data.recentTransactions.length > 0) {
        transactionsHtml = data.recentTransactions.map(t => {
            const amountClass = t.direction === '+' ? 'transaction-amount-positive' : t.direction === '-' ? 'transaction-amount-negative' : 'transaction-amount-neutral';
            return `<div class="transaction-item"><div><div class="text-on-surface text-sm">${escapeHtml(t.description)}</div><div class="text-secondary text-xs mt-0.5">${formatDate(t.date)}</div></div><div class="data-mono ${amountClass}" dir="ltr">${t.direction}${formatNumber(t.amount)} ${escapeHtml(t.unit)}</div></div>`;
        }).join('');
    } else {
        transactionsHtml = '<div class="text-secondary text-sm text-center py-4">لا توجد عمليات</div>';
    }
    const statusBadge = data.isActive ? '<span class="status-active">نشط</span>' : '<span class="status-stopped">متوقف</span>';
    container.innerHTML = `
        <div class="p-4 border-b border-outline-variant flex justify-between items-start bg-surface-container-low/50">
            <div class="flex items-center gap-3">
                <div class="supplier-avatar-lg">${data.name.charAt(0)}</div>
                <div><h3 class="text-lg font-bold text-on-surface">${escapeHtml(data.name)}</h3><div class="flex items-center gap-2 mt-1"><span class="text-secondary text-xs">${formatDate(data.createdAt)}</span><span class="w-1 h-1 rounded-full bg-outline-variant"></span>${statusBadge}</div></div>
            </div>
            <div class="flex items-center gap-1">
                <a href="/Suppliers/AddOrUpdate/${data.id}" ajaxType="GET" title="تعديل" class="openModal p-2 text-secondary hover:text-gold hover:bg-gold/5 rounded-lg transition-colors">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                </a>
                <button onclick="toggleActive('${data.id}')" class="p-2 text-secondary hover:text-gold hover:bg-gold/5 rounded-lg transition-colors" title="${data.isActive ? 'إلغاء التفعيل' : 'تفعيل'}">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="${data.isActive ? 'M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636' : 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z'}"/></svg>
                </button>
            </div>
        </div>
        <div class="flex-1 overflow-y-auto p-4 flex flex-col gap-4">
            <div class="kpi-gold"><div class="flex items-center justify-between mb-2"><span class="text-secondary text-xs font-medium">رصيد الذهب الحالي</span><span class="material-symbols-outlined text-primary text-[20px]">grid_goldenratio</span></div><div class="flex items-end gap-2"><span class="text-2xl font-bold text-on-surface tracking-tight" dir="ltr">${goldDisplay}</span><span class="text-secondary text-sm mb-1">جم عيار 21</span></div>${goldHint}</div>
            <div class="kpi-finance"><div class="flex items-center justify-between mb-2"><span class="text-secondary text-xs font-medium">أجور التصنيع المستحقة</span><span class="material-symbols-outlined text-secondary text-[20px]">payments</span></div><div class="flex items-end gap-2"><span class="text-xl font-bold text-on-surface tracking-tight" dir="ltr">${mfgDisplay}</span><span class="text-secondary text-xs mb-1">د.إ</span></div>${mfgHint}</div>
            <div class="kpi-finance" style="border-right: 3px solid var(--color-primary);"><div class="flex items-center justify-between mb-2"><span class="text-secondary text-xs font-medium">المعاملات المالية</span><span class="material-symbols-outlined text-primary text-[20px]">account_balance</span></div>${finHtml}</div>
            <div><h4 class="text-secondary text-xs font-medium uppercase tracking-wider mb-2">معلومات التواصل</h4><div class="bg-surface-container rounded-lg border border-outline-variant p-3 flex flex-col gap-2">
                <div class="contact-item"><svg class="w-4 h-4 text-secondary" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"/></svg><span class="text-on-surface" dir="ltr">${escapeHtml(data.primaryPhone)}</span></div>
                ${data.secondaryPhone ? `<div class="contact-item"><svg class="w-4 h-4 text-secondary" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"/></svg><span class="text-on-surface" dir="ltr">${escapeHtml(data.secondaryPhone)}</span></div>` : ''}
                ${data.bankAccountNumber ? `<div class="contact-item"><svg class="w-4 h-4 text-secondary" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/></svg><span class="text-on-surface">${escapeHtml(data.bankAccountNumber)}</span></div>` : ''}
            </div></div>
            <div><div class="flex justify-between items-center mb-2"><h4 class="text-secondary text-xs font-medium uppercase tracking-wider">آخر العمليات</h4></div><div class="flex flex-col gap-2">${transactionsHtml}</div></div>
        </div>`;
}

// ── Mobile Slide-Over Detail ─────────────────────────
async function openMobileDetail(id) {
    const panel = document.getElementById('mobileDetailPanel');
    const overlay = document.getElementById('mobileDetailOverlay');
    const content = document.getElementById('mobileDetailContent');
    content.innerHTML = '<div class="flex items-center justify-center py-12"><div class="text-secondary">جاري التحميل...</div></div>';
    overlay.classList.remove('hidden');
    panel.classList.add('open');
    try {
        const response = await fetch(`/Suppliers/GetById?id=${id}`);
        const data = await response.json();
        if (!data.success) { content.innerHTML = `<div class="text-error p-4">حدث خطأ: ${escapeHtml(data.error)}</div>`; return; }
        renderMobileDetail(content, data);
    } catch (e) { content.innerHTML = '<div class="text-error p-4">حدث خطأ أثناء التحميل</div>'; }
}

function renderMobileDetail(container, data) {
    const statusBadge = data.isActive ? '<span class="status-active">نشط</span>' : '<span class="status-stopped">متوقف</span>';
    const goldDisplay = formatNumber(Math.abs(data.goldBalance));
    const mfgDisplay = formatNumber(Math.abs(data.manufacturingBalance));
    const goldHint = balanceHintHtml(data.goldBalance);
    const mfgHint = balanceHintHtml(data.manufacturingBalance);
    let mobileFinHtml = '';
    if (data.financialBalancesByCurrency && data.financialBalancesByCurrency.length > 0) {
        mobileFinHtml = data.financialBalancesByCurrency.map(c => {
            const cls = c.balance < 0 ? 'text-error' : c.balance > 0 ? 'text-success' : 'text-on-surface';
            const sign = c.balance > 0 ? 'له' : c.balance < 0 ? 'لنا' : '';
            return `<div class="flex items-center justify-between py-0.5">
                <span class="text-xs text-secondary">${escapeHtml(c.currency)}</span>
                <span class="data-mono text-sm font-medium ${cls}" dir="ltr">${formatNumber(Math.abs(c.balance))} <span class="text-xs">${sign}</span></span>
            </div>`;
        }).join('');
    } else {
        mobileFinHtml = '<div class="text-secondary text-xs text-center py-2">لا توجد معاملات مالية</div>';
    }
    container.innerHTML = `
        <div class="flex items-center gap-3 mb-4">
            <div class="supplier-avatar-lg">${data.name.charAt(0)}</div>
            <div><h3 class="text-lg font-bold text-on-surface">${escapeHtml(data.name)}</h3><div class="flex items-center gap-2 mt-1"><span class="text-secondary text-xs">${formatDate(data.createdAt)}</span><span class="w-1 h-1 rounded-full bg-outline-variant"></span>${statusBadge}</div></div>
        </div>
        <div class="flex gap-2 mb-4">
            <a href="/Suppliers/AddOrUpdate/${data.id}" ajaxType="GET" title="تعديل" class="openModal flex-1 px-4 py-2 border border-outline-variant rounded-lg text-sm font-medium text-on-surface hover:bg-surface-container transition-colors text-center" onclick="closeMobileDetail();">تعديل</a>
            <button onclick="toggleActive('${data.id}')" class="flex-1 px-4 py-2 border border-outline-variant rounded-lg text-sm font-medium text-on-surface hover:bg-surface-container transition-colors">${data.isActive ? 'إلغاء التفعيل' : 'تفعيل'}</button>
        </div>
        <div class="kpi-gold mb-3"><div class="flex items-center justify-between mb-1"><span class="text-secondary text-xs">رصيد الذهب</span></div><div class="data-mono text-lg text-on-surface" dir="ltr">${goldDisplay} جم</div>${goldHint}</div>
        <div class="kpi-finance mb-3"><div class="flex items-center justify-between mb-1"><span class="text-secondary text-xs">أجور التصنيع</span></div><div class="data-mono text-on-surface text-lg" dir="ltr">${mfgDisplay} د.إ</div>${mfgHint}</div>
        <div class="kpi-finance mb-3" style="border-right: 3px solid var(--color-primary);"><div class="flex items-center justify-between mb-1"><span class="text-secondary text-xs">المعاملات المالية</span></div>${mobileFinHtml}</div>
        <div class="mb-3"><h4 class="text-secondary text-xs font-medium uppercase tracking-wider mb-2">معلومات التواصل</h4><div class="bg-surface-container rounded-lg border border-outline-variant p-3 flex flex-col gap-2">
            <div class="contact-item"><svg class="w-4 h-4 text-secondary" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"/></svg><span class="text-on-surface" dir="ltr">${escapeHtml(data.primaryPhone)}</span></div>
            ${data.secondaryPhone ? `<div class="contact-item"><svg class="w-4 h-4 text-secondary" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"/></svg><span dir="ltr">${escapeHtml(data.secondaryPhone)}</span></div>` : ''}
            ${data.bankAccountNumber ? `<div class="contact-item"><svg class="w-4 h-4 text-secondary" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/></svg><span class="text-on-surface">${escapeHtml(data.bankAccountNumber)}</span></div>` : ''}
        </div></div>`;
}

function closeMobileDetail() {
    const panel = document.getElementById('mobileDetailPanel');
    const overlay = document.getElementById('mobileDetailOverlay');
    panel.classList.remove('open');
    overlay.classList.add('hidden');
}

// ── Helper Functions ────────────────────────────────
function balanceHintHtml(balance) {
    if (balance === 0) return '';
    if (balance > 0) {
        return '<div class="flex items-center gap-1 mt-1 text-xs font-medium" style="color:#ba1a1a"><span class="material-symbols-outlined text-[16px]">arrow_downward</span>مطلوب للدفع</div>';
    }
    return '<div class="flex items-center gap-1 mt-1 text-xs font-medium" style="color:#006c49"><span class="material-symbols-outlined text-[16px]">arrow_upward</span>رصيد لنا</div>';
}

function formatNumber(num) {
    return Number(num).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 3 });
}

function formatDate(dateStr) {
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now - date;
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    if (days === 0) return 'اليوم';
    if (days === 1) return 'أمس';
    if (days < 7) return `منذ ${days} أيام`;
    return date.toLocaleDateString('ar-JO', { year: 'numeric', month: 'short', day: 'numeric' });
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function autoSelectFirst() {
    if (allSuppliers.length > 0 && !selectedSupplierId) {
        selectSupplier(allSuppliers[0].id);
    }
}

// ── Toggle Active ───────────────────────────────────
async function toggleActive(supplierId) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    try {
        const response = await fetch('/Suppliers/ToggleActive', {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `id=${supplierId}&__RequestVerificationToken=${encodeURIComponent(token)}`
        });
        const data = await response.json();
        renderToastFromController(data);
    } catch {
        showToastMessage('حدث خطأ أثناء الاتصال بالخادم', 'error', 'إدارة الموردين');
    }
}

// ── Initialize ───────────────────────────────────────
$(document).ready(function () {
    loadSupplierTable();
});