/* ─── Salary Payments JS ─────────────────── */

let currentPage = 1;
const pageSize = 20;

function formatAmount(amount) {
    return `${Number(amount).toFixed(3)}`;
}

function formatDate(dateStr) {
    const d = new Date(dateStr + 'T00:00:00');
    return d.toLocaleDateString('ar-JO', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

// ─── Init ────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    loadSalaryPayments();
});

// ─── Payments Table ──────────────────────
async function loadSalaryPayments(page = 1) {
    currentPage = page;
    const employeeName = document.getElementById('filterEmployeeName')?.value || '';
    const fromDate = document.getElementById('filterFromDate')?.value || '';
    const toDate = document.getElementById('filterToDate')?.value || '';

    const params = new URLSearchParams({
        page: page,
        pageSize: pageSize,
        ...(employeeName && { employeeName }),
        ...(fromDate && { fromDate }),
        ...(toDate && { toDate })
    });

    try {
        const res = await fetch(`/SalaryPayments/GetPaged?${params}`);
        const data = await res.json();

        if (data.success === false) {
            toastr.error(data.error || 'حدث خطأ', 'دفعات الرواتب');
            return;
        }

        const tbody = document.getElementById('salaryPaymentsBody');
        if (!data.items || data.items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="py-12 text-center text-secondary">لا توجد دفعات رواتب</td></tr>';
            document.getElementById('paginationContainer').style.display = 'none';
            return;
        }

        tbody.innerHTML = data.items.map(p => {
            const status = p.isOnSchedule
                ? '<span class="px-2 py-0.5 rounded-full bg-success/10 text-success text-xs font-medium">في الموعد</span>'
                : `<span class="px-2 py-0.5 rounded-full bg-error/10 text-error text-xs font-medium">متأخر (خصم ${formatAmount(p.discountAmount)})</span>`;
            return `
                <tr class="border-b border-outline-variant/50 hover:bg-[rgba(212,175,55,0.06)] even:bg-black/[0.02]">
                    <td class="px-md py-3">
                        <div class="font-medium text-on-surface">${p.employeeName || '—'}</div>
                    </td>
                    <td class="px-md py-3">${formatDate(p.paymentDate)}</td>
                    <td class="px-md py-3">${formatDate(p.scheduledDate)}</td>
                    <td class="px-md py-3 font-data-mono text-left">${formatAmount(p.salaryAmount)}</td>
                    <td class="px-md py-3 font-data-mono text-left ${p.discountAmount > 0 ? 'text-error' : 'text-secondary'}">${formatAmount(p.discountAmount)}</td>
                    <td class="px-md py-3 font-data-mono text-left text-on-surface font-semibold">${formatAmount(p.amount)}</td>
                    <td class="px-md py-3">${p.accountName || '—'}</td>
                    <td class="px-md py-3">${status}</td>
                </tr>`;
        }).join('');

        renderPagination(data.totalCount, data.page, data.pageSize);
    } catch (e) {
        console.error('Failed to load salary payments', e);
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
    info.textContent = `${totalCount} دفعة — صفحة ${page} من ${totalPages}`;

    let html = '';
    if (page > 1) {
        html += `<button class="px-sm py-1 bg-surface-container border border-outline-variant rounded font-label-md text-label-md hover:bg-surface-container-high transition-colors" onclick="loadSalaryPayments(${page - 1})">السابق</button>`;
    }
    if (page < totalPages) {
        html += `<button class="px-sm py-1 bg-primary-container text-on-primary-container rounded font-label-md text-label-md hover:bg-inverse-primary transition-colors" onclick="loadSalaryPayments(${page + 1})">التالي</button>`;
    }
    buttons.innerHTML = html;
}

// ─── Filters ──────────────────────────────
document.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && e.target && e.target.id === 'filterEmployeeName') {
        loadSalaryPayments();
    }
});
