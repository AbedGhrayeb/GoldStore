/* ─── Expenses JS ──────────────────────────── */

let currentPage = 1;
const pageSize = 20;
let categories = [];
let allAccounts = [];

function formatAmount(amount, currencySymbol) {
    return `${amount.toFixed(3)} ${currencySymbol}`;
}

function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString('ar-JO', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

// ─── Init ────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadKpis();
    loadCategories();
    loadAccounts();
    loadExpenses();
});

// ─── KPIs ───────────────────────────────────

async function loadKpis() {
    try {
        const res = await fetch('/Expenses/GetKpis');
        const data = await res.json();
        if (!data) return;

        renderCurrencyList('kpiTodayTotals', data.todayTotals);
        renderCurrencyList('kpiMonthTotals', data.monthTotals);

        document.getElementById('kpiTopCategory').textContent = data.topCategoryName ?? '—';
        renderCurrencyList('kpiTopCategoryAmounts', data.topCategoryAmounts);
    } catch (e) {
        console.error('Failed to load KPIs', e);
    }
}

function renderCurrencyList(containerId, items) {
    const container = document.getElementById(containerId);
    if (!items || items.length === 0) {
        container.innerHTML = '<div class="font-data-mono text-data-mono text-secondary">0.000</div>';
        return;
    }
    container.innerHTML = items.map(item =>
        `<div class="font-data-mono text-data-mono text-on-surface">${item.amount.toFixed(3)} ${item.symbol}</div>`
    ).join('');
}

// ─── Categories ─────────────────────────────

async function loadCategories() {
    try {
        const res = await fetch('/Expenses/GetCategories?activeOnly=false');
        const data = await res.json();
        if (Array.isArray(data)) {
            categories = data;
            populateCategoryDropdowns();
        }
    } catch (e) {
        console.error('Failed to load categories', e);
    }
}

function populateCategoryDropdowns() {
    const filterSelect = document.getElementById('filterCategoryId');
    const formSelect = document.getElementById('expenseCategoryId');

    const filterVal = filterSelect.value;
    const formVal = formSelect.value;

    filterSelect.innerHTML = '<option value="">الكل</option>';
    formSelect.innerHTML = '<option value="">بدون تصنيف</option>';

    categories.forEach(c => {
        filterSelect.innerHTML += `<option value="${c.id}">${c.name}</option>`;
        formSelect.innerHTML += `<option value="${c.id}">${c.name}</option>`;
    });

    filterSelect.value = filterVal;
    formSelect.value = formVal;
}

async function loadAccounts() {
    try {
        const res = await fetch('/FinancialAccounts/GetCashAccounts');
        const data = await res.json();

        const accountSelect = document.getElementById('expenseAccountId');
        accountSelect.innerHTML = '<option value="">اختر الحساب...</option>';

        if (Array.isArray(data)) {
            allAccounts = data;
            data.forEach(a => {
                const symbol = getCurrencySymbol(a.currency);
                accountSelect.innerHTML += `<option value="${a.id}">${a.name} (${symbol} ${a.balance?.toFixed(3) ?? '0.000'})</option>`;
            });
        }

        const bankRes = await fetch('/FinancialAccounts/GetBankAccounts');
        const bankData = await bankRes.json();
        if (Array.isArray(bankData)) {
            bankData.forEach(a => {
                const symbol = getCurrencySymbol(a.currency);
                accountSelect.innerHTML += `<option value="${a.id}">${a.name} (${symbol} ${a.balance?.toFixed(3) ?? '0.000'})</option>`;
            });
        }
    } catch (e) {
        console.error('Failed to load accounts', e);
    }
}

function getCurrencySymbol(currency) {
    switch (currency) {
        case 'JOD': return 'د.إ';
        case 'USD': return '$';
        case 'ILS': return '₪';
        default: return currency;
    }
}

// ─── Expenses Table ─────────────────────────

async function loadExpenses(page = 1) {
    currentPage = page;
    const accountName = document.getElementById('filterAccountName')?.value || '';
    const fromDate = document.getElementById('filterFromDate')?.value || '';
    const toDate = document.getElementById('filterToDate')?.value || '';
    const categoryId = document.getElementById('filterCategoryId')?.value || '';

    const params = new URLSearchParams({
        page: page,
        pageSize: pageSize,
        ...(accountName && { accountName }),
        ...(fromDate && { fromDate }),
        ...(toDate && { toDate }),
        ...(categoryId && { categoryId })
    });

    try {
        const res = await fetch(`/Expenses/GetPaged?${params}`);
        const data = await res.json();

        const tbody = document.getElementById('expensesBody');
        if (!data.items || data.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="py-12 text-center text-secondary">لا توجد مصروفات</td></tr>';
            document.getElementById('paginationContainer').style.display = 'none';
            return;
        }

        tbody.innerHTML = data.items.map(e => `
            <tr class="border-b border-outline-variant/50 hover:bg-[rgba(212,175,55,0.06)] even:bg-black/[0.02]">
                <td class="px-md py-3">${formatDate(e.expenseDate)}</td>
                <td class="px-md py-3">${e.categoryName}</td>
                <td class="px-md py-3">${e.description ?? '—'}</td>
                <td class="px-md py-3 font-data-mono text-left">${formatAmount(e.amount, e.currencySymbol)}</td>
                <td class="px-md py-3">${e.accountName || '—'}</td>
                <td class="px-md py-3 text-center">
                    <button class="text-primary hover:text-primary-fixed transition-colors" onclick="editExpense('${e.id}')" title="تعديل">
                        <span class="material-symbols-outlined text-[18px]">edit</span>
                    </button>
                    <button class="text-error hover:text-red-700 transition-colors mr-sm" onclick="deleteExpense('${e.id}')" title="حذف">
                        <span class="material-symbols-outlined text-[18px]">delete</span>
                    </button>
                </td>
            </tr>
        `).join('');

        renderPagination(data.totalCount, data.page, data.pageSize);
    } catch (e) {
        console.error('Failed to load expenses', e);
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
    info.textContent = `${totalCount} مصروف — صفحة ${page} من ${totalPages}`;

    let html = '';
    if (page > 1) {
        html += `<button class="px-sm py-1 bg-surface-container border border-outline-variant rounded font-label-md text-label-md hover:bg-surface-container-high transition-colors" onclick="loadExpenses(${page - 1})">السابق</button>`;
    }
    if (page < totalPages) {
        html += `<button class="px-sm py-1 bg-primary-container text-on-primary-container rounded font-label-md text-label-md hover:bg-inverse-primary transition-colors" onclick="loadExpenses(${page + 1})">التالي</button>`;
    }
    buttons.innerHTML = html;
}

// ─── Create/Edit Expense Modal ──────────────

function openCreateExpenseModal() {
    document.getElementById('expenseId').value = '';
    document.getElementById('expenseModalTitle').textContent = 'إضافة مصروف';
    document.getElementById('btnSubmitExpense').textContent = 'إضافة المصروف';
    document.getElementById('expenseForm').reset();
    document.getElementById('expenseDate').value = new Date().toISOString().split('T')[0];
    document.getElementById('expenseModal').classList.remove('hidden');
}

async function editExpense(id) {
    try {
        const res = await fetch(`/Expenses/GetPaged?page=1&pageSize=1000`);
        const data = await res.json();
        const expense = data.items?.find(e => e.id === id);
        if (!expense) return;

        document.getElementById('expenseId').value = expense.id;
        document.getElementById('expenseModalTitle').textContent = 'تعديل المصروف';
        document.getElementById('btnSubmitExpense').textContent = 'تحديث المصروف';
        document.getElementById('expenseDate').value = expense.expenseDate;
        document.getElementById('expenseCategoryId').value = expense.categoryId ?? '';
        document.getElementById('expenseDescription').value = expense.description ?? '';
        document.getElementById('expenseAmount').value = expense.amount;
        document.getElementById('expenseAccountId').value = expense.accountId;
        document.getElementById('expenseModal').classList.remove('hidden');
    } catch (e) {
        console.error('Failed to load expense', e);
    }
}

function closeExpenseModal() {
    document.getElementById('expenseModal').classList.add('hidden');
}

async function submitExpense() {
    const id = document.getElementById('expenseId').value;
    const dateVal = document.getElementById('expenseDate').value;
    const categoryId = document.getElementById('expenseCategoryId').value;
    const description = document.getElementById('expenseDescription').value;
    const amount = parseFloat(document.getElementById('expenseAmount').value);
    const accountId = document.getElementById('expenseAccountId').value;

    if (!dateVal || isNaN(amount) || amount <= 0 || !accountId) {
        toastr.error('يرجى تعبئة جميع الحقول المطلوبة');
        return;
    }

    const payload = {
        expenseDate: dateVal,
        categoryId: categoryId || null,
        description: description || null,
        amount: amount,
        accountId: accountId
    };

    if (id) {
        payload.id = id;
    }

    const url = id ? '/Expenses/Update' : '/Expenses/Create';
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    try {
        const res = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        });

        const result = await res.json();

        if (result.status === 1) {
            toastr.success(result.msg, result.management);
            closeExpenseModal();
            loadExpenses(currentPage);
            loadKpis();
        } else {
            toastr.error(result.msg || 'حدث خطأ', result.management || 'المصروفات');
        }
    } catch (e) {
        toastr.error('حدث خطأ في الاتصال', 'المصروفات');
    }
}

async function deleteExpense(id) {
    if (!confirm('هل أنت متأكد من حذف هذا المصروف؟')) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const formData = new FormData();
    formData.append('id', id);

    try {
        const res = await fetch('/Expenses/Delete', {
            method: 'POST',
            headers: { 'RequestVerificationToken': token },
            body: formData
        });

        const result = await res.json();
        if (result.status === 1) {
            toastr.success(result.msg, result.management);
            loadExpenses(currentPage);
            loadKpis();
        } else {
            toastr.error(result.msg, result.management);
        }
    } catch (e) {
        toastr.error('حدث خطأ في الاتصال', 'المصروفات');
    }
}

// ─── Category Manager ───────────────────────

function openCategoryManager() {
    renderCategoriesList();
    document.getElementById('categoryModal').classList.remove('hidden');
}

function closeCategoryManager() {
    document.getElementById('categoryModal').classList.add('hidden');
}

function renderCategoriesList() {
    const list = document.getElementById('categoriesList');
    if (categories.length === 0) {
        list.innerHTML = '<p class="text-secondary text-center py-sm">لا توجد تصنيفات</p>';
        return;
    }

    list.innerHTML = categories.map(c => `
        <div class="flex items-center justify-between bg-surface border border-outline-variant rounded-lg px-sm py-2" data-category-id="${c.id}">
            <div class="flex-1 min-w-0">
                <span class="category-name font-body-md text-body-md text-on-surface ${c.isActive ? '' : 'line-through text-secondary'}" data-id="${c.id}">${c.name}</span>
                <input type="text" class="category-edit-input hidden w-full bg-surface-container border border-primary rounded py-1 px-2 font-body-md text-body-md focus:outline-none focus:ring-1 focus:ring-primary" data-id="${c.id}" value="${c.name}" />
            </div>
            <div class="flex gap-xs mr-sm">
                <button class="btn-edit-category text-primary hover:text-primary-fixed transition-colors" onclick="startInlineEditCategory('${c.id}')" title="تعديل">
                    <span class="material-symbols-outlined text-[18px]">edit</span>
                </button>
                <button class="btn-save-category hidden text-success hover:text-green-700 transition-colors" onclick="saveInlineEditCategory('${c.id}')" title="حفظ">
                    <span class="material-symbols-outlined text-[18px]">check</span>
                </button>
                <button class="btn-cancel-category hidden text-secondary hover:text-on-surface transition-colors" onclick="cancelInlineEditCategory('${c.id}')" title="إلغاء">
                    <span class="material-symbols-outlined text-[18px]">close</span>
                </button>
                <button class="text-error hover:text-red-700 transition-colors" onclick="deleteCategory('${c.id}')" title="حذف">
                    <span class="material-symbols-outlined text-[18px]">delete</span>
                </button>
            </div>
        </div>
    `).join('');

    list.querySelectorAll('.category-edit-input').forEach(input => {
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') saveInlineEditCategory(input.dataset.id);
            if (e.key === 'Escape') cancelInlineEditCategory(input.dataset.id);
        });
    });
}

function startInlineEditCategory(id) {
    const row = document.querySelector(`[data-category-id="${id}"]`);
    const nameSpan = row.querySelector('.category-name');
    const editInput = row.querySelector('.category-edit-input');
    const btnEdit = row.querySelector('.btn-edit-category');
    const btnSave = row.querySelector('.btn-save-category');
    const btnCancel = row.querySelector('.btn-cancel-category');

    editInput.value = nameSpan.textContent.trim();
    nameSpan.classList.add('hidden');
    editInput.classList.remove('hidden');
    btnEdit.classList.add('hidden');
    btnSave.classList.remove('hidden');
    btnCancel.classList.remove('hidden');
    editInput.focus();
    editInput.select();
}

function cancelInlineEditCategory(id) {
    const row = document.querySelector(`[data-category-id="${id}"]`);
    const nameSpan = row.querySelector('.category-name');
    const editInput = row.querySelector('.category-edit-input');
    const btnEdit = row.querySelector('.btn-edit-category');
    const btnSave = row.querySelector('.btn-save-category');
    const btnCancel = row.querySelector('.btn-cancel-category');

    nameSpan.classList.remove('hidden');
    editInput.classList.add('hidden');
    btnEdit.classList.remove('hidden');
    btnSave.classList.add('hidden');
    btnCancel.classList.add('hidden');
}

async function saveInlineEditCategory(id) {
    const row = document.querySelector(`[data-category-id="${id}"]`);
    const editInput = row.querySelector('.category-edit-input');
    const newName = editInput.value.trim();

    if (!newName) {
        toastr.error('اسم التصنيف مطلوب');
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const res = await fetch('/Expenses/UpdateCategory', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ id, name: newName })
    });

    const result = await res.json();
    if (result.status === 1) {
        toastr.success(result.msg, result.management);
        await loadCategories();
        renderCategoriesList();
        loadExpenses(currentPage);
        loadKpis();
    } else {
        toastr.error(result.msg, result.management);
    }
}

async function createCategory() {
    const name = document.getElementById('newCategoryName').value.trim();
    if (!name) {
        toastr.error('اسم التصنيف مطلوب');
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const res = await fetch('/Expenses/CreateCategory', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ name })
    });

    const result = await res.json();
    if (result.status === 1) {
        toastr.success(result.msg, result.management);
        document.getElementById('newCategoryName').value = '';
        await loadCategories();
        renderCategoriesList();
        loadExpenses(currentPage);
        loadKpis();
    } else {
        toastr.error(result.msg, result.management);
    }
}

async function deleteCategory(id) {
    if (!confirm('هل أنت متأكد من حذف هذا التصنيف؟')) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const formData = new FormData();
    formData.append('id', id);

    const res = await fetch('/Expenses/DeleteCategory', {
        method: 'POST',
        headers: { 'RequestVerificationToken': token },
        body: formData
    });

    const result = await res.json();
    if (result.status === 1) {
        toastr.success(result.msg, result.management);
        await loadCategories();
        renderCategoriesList();
        loadExpenses(currentPage);
        loadKpis();
    } else {
        toastr.error(result.msg, result.management);
    }
}

// ─── Toast callback for table refresh ──────

window.refreshExpenseTable = function () {
    loadExpenses(currentPage);
    loadKpis();
};

window.refreshCategories = function () {
    loadCategories();
};