/* ═══════════════════════════════════════════════════════
   EMPLOYEES PAGE — JavaScript
   Pattern: loadDataFromUrl, openModal, ToastResult callback refresh
   ═══════════════════════════════════════════════════════ */

let allEmployees = [];
let currentFilter = 'all';
let currentSearch = '';

// ── Load Employee Table from JSON ────────────────────
function loadEmployeeTable() {
    $.ajax({
        url: '/Employees/List',
        type: 'GET',
        dataType: 'json',
        contentType: 'application/json',
        success: function (data) {
            allEmployees = Array.isArray(data) ? data : [];
            renderTable();
            updateCounts();
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل الموظفين', 'error', 'إدارة الموظفين');
            const tbody = document.getElementById('employeesTableBody');
            const cards = document.getElementById('employeesCards');
            if (tbody) tbody.innerHTML = '<tr><td colspan="6" class="text-center py-12 text-red-500">فشل تحميل البيانات. <a href="#" onclick="loadEmployeeTable(); return false;" class="text-gold underline">إعادة المحاولة</a></td></tr>';
            if (cards) cards.innerHTML = '<div class="text-center py-12 text-red-500">فشل تحميل البيانات</div>';
        }
    });
}

// ── Refresh (callback from ToastResult) ──────────────
function refreshEmployeeTable() {
    $.ajax({
        url: '/Employees/List',
        type: 'GET',
        dataType: 'json',
        contentType: 'application/json',
        success: function (data) {
            allEmployees = Array.isArray(data) ? data : [];
            renderTable();
            updateCounts();
            reapplyFilter();
        }
    });
}

function renderTable() {
    const tbody = document.getElementById('employeesTableBody');
    const cardsContainer = document.getElementById('employeesCards');
    const emptyDesktop = document.getElementById('emptyStateDesktop');
    const emptyMobile = document.getElementById('emptyStateMobile');

    if (allEmployees.length === 0) {
        if (tbody) tbody.innerHTML = '';
        if (cardsContainer) cardsContainer.innerHTML = '';
        if (emptyDesktop) emptyDesktop.classList.remove('hidden');
        if (emptyMobile) emptyMobile.classList.remove('hidden');
        return;
    }

    if (emptyDesktop) emptyDesktop.classList.add('hidden');
    if (emptyMobile) emptyMobile.classList.add('hidden');

    if (tbody) tbody.innerHTML = allEmployees.map(e => renderDesktopRow(e)).join('');
    if (cardsContainer) cardsContainer.innerHTML = allEmployees.map(e => renderMobileCard(e)).join('');
}

function renderDesktopRow(e) {
    const statusClass = e.isActive ? 'status-active' : 'status-stopped';
    const statusText = e.isActive ? 'نشط' : 'متوقف';
    const initial = e.firstName ? e.firstName.charAt(0) : '?';
    const userHtml = e.userEmail
        ? `<span class="text-xs text-secondary" dir="ltr">${escapeHtml(e.userEmail)}</span>`
        : '<span class="text-xs text-gray-400">غير مرتبط</span>';

    return `<tr class="hover:bg-primary-fixed/10 transition-colors cursor-default"
                data-employee-id="${e.id}" data-is-active="${e.isActive}" data-search-text="${e.fullName} ${e.roleName} ${e.userEmail || ''}">
            <td class="py-3 px-4">
                <div class="flex items-center gap-3">
                    <div class="employee-avatar">${initial}</div>
                    <div>
                        <div class="text-on-surface font-semibold">${escapeHtml(e.fullName)}</div>
                        <div class="text-secondary text-xs" dir="ltr">${escapeHtml(e.salaryCycleName)}</div>
                    </div>
                </div>
            </td>
            <td class="py-3 px-4 text-on-surface">${escapeHtml(e.roleName)}</td>
            <td class="py-3 px-4 data-mono text-on-surface" dir="ltr">${formatNumber(e.salary)}</td>
            <td class="py-3 px-4">${userHtml}</td>
            <td class="py-3 px-4"><span class="${statusClass}">${statusText}</span></td>
            <td class="py-3 px-4">
                <div class="flex items-center gap-1">
                    <a href="/Employees/AddOrUpdate/${e.id}" ajaxType="GET" title="تعديل"
                       class="openModal p-1.5 text-gray-400 hover:text-gold hover:bg-gold/5 rounded-lg transition-colors">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                    </a>
                    <button onclick="toggleActive('${e.id}')"
                            class="p-1.5 ${e.isActive ? 'text-green-500 hover:text-red-500 hover:bg-red-50' : 'text-red-400 hover:text-green-500 hover:bg-green-50'} rounded-lg transition-colors"
                            title="${e.isActive ? 'إلغاء التفعيل' : 'تفعيل'}">
                        ${e.isActive
                            ? '<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"/></svg>'
                            : '<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>'}
                    </button>
                </div>
            </td>
        </tr>`;
}

function renderMobileCard(e) {
    const statusClass = e.isActive ? 'status-active' : 'status-stopped';
    const statusText = e.isActive ? 'نشط' : 'متوقف';
    const initial = e.firstName ? e.firstName.charAt(0) : '?';

    return `<div class="employee-mobile-card" data-employee-id="${e.id}" data-is-active="${e.isActive}" data-search-text="${e.fullName} ${e.roleName} ${e.userEmail || ''}">
            <div class="flex items-center justify-between">
                <div class="flex items-center gap-3">
                    <div class="employee-avatar">${initial}</div>
                    <div>
                        <div class="text-on-surface font-semibold">${escapeHtml(e.fullName)}</div>
                        <div class="text-secondary text-xs">${escapeHtml(e.roleName)}</div>
                    </div>
                </div>
                <div class="flex items-center gap-2">
                    <a href="/Employees/AddOrUpdate/${e.id}" ajaxType="GET" title="تعديل"
                       class="openModal p-2 text-gray-400 hover:text-gold hover:bg-gold/5 rounded-lg transition-colors">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                    </a>
                    <span class="${statusClass}">${statusText}</span>
                </div>
            </div>
            <div class="mt-3 flex gap-4 items-center">
                <div>
                    <div class="text-secondary text-xs">الراتب</div>
                    <div class="data-mono text-on-surface" dir="ltr">${formatNumber(e.salary)}</div>
                </div>
                <div>
                    <div class="text-secondary text-xs">حساب المستخدم</div>
                    <div class="text-xs ${e.userEmail ? 'text-secondary' : 'text-gray-400'}" dir="ltr">${e.userEmail ? escapeHtml(e.userEmail) : 'غير مرتبط'}</div>
                </div>
            </div>
        </div>`;
}

// ── Filter Tabs ──────────────────────────────────────
function updateCounts() {
    const all = allEmployees.length;
    const active = allEmployees.filter(e => e.isActive).length;
    const stopped = all - active;
    const countAll = document.getElementById('countAll');
    const countActive = document.getElementById('countActive');
    const countStopped = document.getElementById('countStopped');
    if (countAll) countAll.textContent = all;
    if (countActive) countActive.textContent = active;
    if (countStopped) countStopped.textContent = stopped;
}

function filterEmployees(status) {
    currentFilter = status;
    const tabs = document.querySelectorAll('.filter-tab');
    tabs.forEach(t => t.classList.remove('active'));
    if (event && event.currentTarget) event.currentTarget.classList.add('active');
    reapplyFilter();
}

function reapplyFilter() {
    searchEmployees();
}

function searchEmployees() {
    currentSearch = document.getElementById('employeeSearch')?.value.trim().toLowerCase() || '';
    const rows = document.querySelectorAll('.employee-table tbody tr');
    const cards = document.querySelectorAll('.employee-mobile-card');
    let visibleCount = 0;

    rows.forEach(row => {
        if (row.querySelector('.py-12')) return;
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

// ── Toggle Active ────────────────────────────────────
async function toggleActive(employeeId) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    try {
        const response = await fetch('/Employees/ToggleActive', {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `id=${employeeId}&__RequestVerificationToken=${encodeURIComponent(token)}`
        });
        const data = await response.json();
        renderToastFromController(data);
    } catch {
        showToastMessage('حدث خطأ أثناء الاتصال بالخادم', 'error', 'إدارة الموظفين');
    }
}

// ── Create Modal: optional user link ─────────────────
function toggleConnectSection() {
    const checkbox = document.getElementById('ConnectToUser');
    const section = document.getElementById('connectUserSection');
    if (!checkbox || !section) return;
    section.classList.toggle('hidden', !checkbox.checked);
    if (checkbox.checked) {
        handleUserLinkMode();
    } else {
        setConnectDisabled(true);
    }
}

function handleUserLinkMode() {
    const mode = document.querySelector('input[name="userLinkMode"]:checked')?.value || 'existing';
    const existingFields = document.getElementById('existingUserFields');
    const newFields = document.getElementById('newUserFields');
    if (existingFields) existingFields.classList.toggle('hidden', mode !== 'existing');
    if (newFields) newFields.classList.toggle('hidden', mode !== 'new');
    setConnectDisabled(false);
}

function setConnectDisabled(disabled) {
    const radios = document.querySelectorAll('input[name="userLinkMode"]');
    const select = document.getElementById('ExistingUserId');
    const email = document.getElementById('NewUserEmail');
    const password = document.getElementById('NewUserPassword');
    radios.forEach(r => { r.disabled = disabled; });
    if (select) select.disabled = disabled;
    if (email) email.disabled = disabled;
    if (password) password.disabled = disabled;
}

// ── Helper Functions ─────────────────────────────────
function formatNumber(num) {
    return Number(num).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 3 });
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ── Initialize ───────────────────────────────────────
$(document).ready(function () {
    loadEmployeeTable();
});
