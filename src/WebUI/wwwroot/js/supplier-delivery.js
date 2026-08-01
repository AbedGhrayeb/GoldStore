/* ═══════════════════════════════════════════════════════
   SUPPLIER DELIVERY WIZARD — JavaScript
   4-step wizard: Supplier → Gold → Manufacturing → Review
   ═══════════════════════════════════════════════════════ */

let currentStep = 1;
let suppliers = [];
let goldLines = [{ karat: 21, weight: 0 }];
let selectedSupplierId = null;

const TOTAL_STEPS = 4;

// ── Page Load ────────────────────────────────────────
$(document).ready(function () {
    loadSuppliers();
    loadKarats();
    loadCurrencies();
});

// ── Load Reference Data ─────────────────────────────
function loadSuppliers() {
    $.ajax({
        url: '/SupplierDeliveries/GetSuppliers',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            suppliers = Array.isArray(data) ? data : [];
            var select = document.getElementById('supplierSelect');
            if (!select) return;
            select.innerHTML = '<option value="">اختر مورد...</option>';
            suppliers.forEach(function (s) {
                var option = document.createElement('option');
                option.value = s.id;
                option.textContent = s.name;
                select.appendChild(option);
            });
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل الموردين', 'error', 'توريد المورد');
        }
    });
}

function loadKarats() {
    $.ajax({
        url: '/SupplierDeliveries/GetKarats',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            window.karatOptions = data;
            renderGoldLines();
        }
    });
}

function loadCurrencies() {
    $.ajax({
        url: '/SupplierDeliveries/GetCurrencies',
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
        }
    });
}

// ── Supplier Selection ────────────────────────────────
function onSupplierSelected() {
    var select = document.getElementById('supplierSelect');
    var errorEl = document.getElementById('supplierError');
    selectedSupplierId = select.value;

    if (!selectedSupplierId) {
        document.getElementById('supplierGoldBalance').value = '';
        document.getElementById('supplierMfgBalance').value = '';
        if (errorEl) errorEl.classList.add('hidden');
        return;
    }

    if (errorEl) errorEl.classList.add('hidden');

    $.ajax({
        url: '/SupplierDeliveries/GetSupplierBalances?id=' + encodeURIComponent(selectedSupplierId),
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            if (data.success === false) {
                showToastMessage(data.error || 'حدث خطأ', 'error', 'توريد المورد');
                return;
            }
            document.getElementById('supplierGoldBalance').value = formatNumber(data.goldBalance) + ' جم عيار 21';
            document.getElementById('supplierMfgBalance').value = formatNumber(data.manufacturingBalance) + ' د.إ';
        },
        error: function () {
            showToastMessage('حدث خطأ أثناء تحميل بيانات المورد', 'error', 'توريد المورد');
        }
    });
}

// ── Gold Lines ───────────────────────────────────────
function renderGoldLines() {
    var container = document.getElementById('goldLines');
    if (!container) return;

    container.innerHTML = '';
    goldLines.forEach(function (line, index) {
        var lineDiv = document.createElement('div');
        lineDiv.className = 'grid grid-cols-1 md:grid-cols-4 gap-4 items-end bg-surface-container-low p-4 rounded-lg border border-outline-variant/30';
        lineDiv.setAttribute('data-line-index', index);

        var karatOptions = (window.karatOptions || []).map(function (k) {
            var selected = k.value === line.karat ? ' selected' : '';
            return '<option value="' + k.value + '"' + selected + '>' + k.label + '</option>';
        }).join('');

        var equivLabel = line.karat === 21 ? line.weight : (line.karat == 24 ? (line.weight / 875 * 1000) : (line*700/875)).toFixed(3);

        lineDiv.innerHTML =
            '<div>' +
                '<label class="block font-label-md text-label-md text-secondary mb-1">العيار</label>' +
                '<select class="gold-karat w-full bg-surface-container-lowest border border-outline rounded-lg py-2 px-3 font-body-lg text-body-lg text-on-surface focus:border-primary-container outline-none" onchange="onKaratChange(' + index + ', this.value)">' +
                    karatOptions +
                '</select>' +
            '</div>' +
            '<div>' +
                '<label class="block font-label-md text-label-md text-secondary mb-1">الوزن الفعلي</label>' +
                '<div class="relative">' +
                    '<input type="number" step="0.01" min="0.01" class="gold-weight w-full bg-surface-container-lowest border border-outline rounded-lg py-2 pl-10 pr-3 font-data-mono text-data-mono text-on-surface focus:border-primary-container outline-none text-left dir-ltr" placeholder="0.00" value="' + (line.weight || '') + '" oninput="onWeightChange(' + index + ', this.value)" />' +
                    '<span class="absolute left-3 top-1/2 -translate-y-1/2 font-label-md text-label-md text-primary">جم</span>' +
                '</div>' +
            '</div>' +
            '<div>' +
                '<label class="block font-label-md text-label-md text-secondary mb-1">الوزن المكافئ (عيار 21)</label>' +
                '<div class="relative">' +
                    '<input type="text" class="gold-equiv w-full bg-surface-variant/50 border border-outline-variant rounded-lg py-2 pl-10 pr-3 font-data-mono text-data-mono text-secondary cursor-not-allowed text-left dir-ltr" disabled value="' + (line.weight > 0 ? equivLabel : '0.00') + '" />' +
                    '<span class="absolute left-3 top-1/2 -translate-y-1/2 font-label-md text-label-md text-secondary">جم</span>' +
                '</div>' +
            '</div>' +
            '<div class="flex items-end">' +
                (goldLines.length > 1
                    ? '<button type="button" onclick="removeGoldLine(' + index + ')" class="w-full p-2 rounded-lg border border-red-200 text-red-600 hover:bg-red-50 transition-colors flex items-center justify-center gap-1"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>حذف</button>'
                    : '') +
            '</div>';

        container.appendChild(lineDiv);
    });
}

function addGoldLine() {
    goldLines.push({ karat: 21, weight: 0 });
    renderGoldLines();
}

function removeGoldLine(index) {
    if (goldLines.length <= 1) return;
    goldLines.splice(index, 1);
    renderGoldLines();
}

function onKaratChange(index, value) {
    goldLines[index].karat = parseInt(value, 10);
    updateEquivalentWeight(index);
}

function onWeightChange(index, value) {
    goldLines[index].weight = parseFloat(value) || 0;
    updateEquivalentWeight(index);
}

function updateEquivalentWeight(index) {
    var line = goldLines[index];
    // var equiv = line.weight > 0 ? (line.weight * line.karat / 21) : 0;
    var equiv = line.weight > 0 ? (line.karat == 24 ? (line.weight / 875) *1000 : line.karat == 18 ? (line.weight * 700) / 875 : line.weight) : 0;
    equiv = Math.round(equiv * 1000) / 1000;

    var lineDiv = document.querySelector('[data-line-index="' + index + '"]');
    if (lineDiv) {
        var equivInput = lineDiv.querySelector('.gold-equiv');
        if (equivInput) equivInput.value = line.weight > 0 ? equiv.toFixed(3) : '0.00';
    }

    if (currentStep === 3) calculateTotalMfg();
}

// ── Manufacturing Fee Calculation ────────────────────
function calculateTotalMfg() {
    var feePerGram = parseFloat(document.getElementById('mfgFeePerGram')?.value) || 0;
    var totalEquiv21 = goldLines.reduce(function (sum, line) {
        return sum + (line.weight > 0 ? (line.karat == 24 ? (line.weight / 875) * 1000 : line.karat == 18 ? (line.weight * 700) / 875 : line.weight) : 0) ;
    }, 0);
    totalEquiv21 = Math.round(totalEquiv21 * 1000) / 1000;

    var totalFee = feePerGram * totalEquiv21;
    var totalFeeEl = document.getElementById('totalMfgFee');
    if (totalFeeEl) totalFeeEl.textContent = totalFee.toFixed(2);
}

// ── Step Navigation ──────────────────────────────────
function goToStep(step) {
    document.querySelectorAll('.delivery-step').forEach(function (el) {
        el.classList.add('hidden');
        el.classList.remove('block');
    });

    var target = document.getElementById('step-' + step);
    if (target) {
        target.classList.remove('hidden');
        target.classList.add('block');
    }

    updateProgressUI(step);
    updateButtons(step);
    currentStep = step;

    if (step === 2) renderGoldLines();
    if (step === 3) calculateTotalMfg();
    if (step === 4) renderReview();
}

function nextStep() {
    if (!validateStep(currentStep)) return;
    if (currentStep < TOTAL_STEPS) {
        goToStep(currentStep + 1);
    }
}

function prevStep() {
    if (currentStep > 1) {
        goToStep(currentStep - 1);
    }
}

function updateProgressUI(step) {
    for (var i = 1; i <= TOTAL_STEPS; i++) {
        var nav = document.getElementById('step-nav-' + i);
        if (!nav) continue;
        var circle = nav.querySelector('div');
        var label = nav.querySelector('span');

        if (i < step) {
            nav.style.opacity = '1';
            circle.classList.remove('bg-surface-variant', 'text-secondary');
            circle.classList.add('bg-green-600', 'text-white');
            circle.innerHTML = '<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/></svg>';
            label.classList.remove('text-secondary');
            label.classList.add('text-on-surface');
        } else if (i === step) {
            nav.style.opacity = '1';
            circle.classList.remove('bg-surface-variant', 'text-secondary', 'bg-green-600', 'text-white');
            circle.classList.add('bg-primary-container', 'text-on-primary-container');
            circle.innerHTML = '' + i;
            label.classList.remove('text-secondary');
            label.classList.add('text-on-surface');
        } else {
            nav.style.opacity = '0.5';
            circle.classList.remove('bg-primary-container', 'text-on-primary-container', 'bg-green-600', 'text-white');
            circle.classList.add('bg-surface-variant', 'text-secondary');
            circle.innerHTML = '' + i;
            label.classList.remove('text-on-surface');
            label.classList.add('text-secondary');
        }
    }

    var progressLine = document.getElementById('progress-line');
    if (progressLine) {
        var pct = ((step - 1) / (TOTAL_STEPS - 1)) * 100;
        progressLine.style.width = pct + '%';
    }
}

function updateButtons(step) {
    var btnPrev = document.getElementById('btn-prev');
    var btnNext = document.getElementById('btn-next');
    var btnSubmit = document.getElementById('btn-submit');
    var spacer = document.getElementById('spacer');

    if (step === 1) {
        btnPrev.classList.add('hidden');
        spacer.classList.remove('hidden');
        btnNext.classList.remove('hidden');
        btnSubmit.classList.add('hidden');
    } else if (step === TOTAL_STEPS) {
        btnPrev.classList.remove('hidden');
        spacer.classList.add('hidden');
        btnNext.classList.add('hidden');
        btnSubmit.classList.remove('hidden');
    } else {
        btnPrev.classList.remove('hidden');
        spacer.classList.add('hidden');
        btnNext.classList.remove('hidden');
        btnSubmit.classList.add('hidden');
    }
}

// ── Validation ────────────────────────────────────────
function validateStep(step) {
    if (step === 1) {
        var supplierId = document.getElementById('supplierSelect')?.value;
        if (!supplierId) {
            var errorEl = document.getElementById('supplierError');
            if (errorEl) errorEl.classList.remove('hidden');
            showToastMessage('يجب اختيار المورد', 'error', 'توريد المورد');
            return false;
        }
        return true;
    }

    if (step === 2) {
        var hasWeight = goldLines.some(function (l) { return l.weight > 0; });
        if (!hasWeight) {
            showToastMessage('يجب إدخال الوزن لصنف واحد على الأقل', 'error', 'توريد المورد');
            return false;
        }
        for (var i = 0; i < goldLines.length; i++) {
            if (goldLines[i].weight <= 0) continue;
            if (goldLines[i].weight <= 0) {
                showToastMessage('الوزن يجب أن يكون أكبر من صفر', 'error', 'توريد المورد');
                return false;
            }
        }
        return true;
    }

    return true;
}

// ── Review ───────────────────────────────────────────
function renderReview() {
    var section = document.getElementById('reviewSection');
    if (!section) return;

    var supplier = suppliers.find(function (s) { return s.id === selectedSupplierId; });
    var supplierName = supplier ? supplier.name : '—';
    var feePerGram = parseFloat(document.getElementById('mfgFeePerGram')?.value) || 0;
    var currency = document.getElementById('mfgCurrency');
    var currencyText = currency ? currency.options[currency.selectedIndex].text : '';

    var totalEquiv21 = 0;
    var linesHtml = goldLines.filter(function (l) { return l.weight > 0; }).map(function (l) {
        var equiv = (l.weight > 0 ? (l.karat == 24 ? (l.weight / 875) * 1000 : l.karat == 18 ? (l.weight * 700) / 875 : l.weight) : 0);
        totalEquiv21 += equiv;
        return '<tr>' +
            '<td class="py-2 px-3 font-body-lg">' + l.karat + ' قيراط</td>' +
            '<td class="py-2 px-3 font-data-mono text-left dir-ltr">' + l.weight.toFixed(2) + ' جم</td>' +
            '<td class="py-2 px-3 font-data-mono text-left dir-ltr">' + equiv.toFixed(3) + ' جم</td>' +
        '</tr>';
    }).join('');

    totalEquiv21 = Math.round(totalEquiv21 * 1000) / 1000;
    var totalMfg = (feePerGram * totalEquiv21).toFixed(2);

    var goldBalanceText = document.getElementById('supplierGoldBalance')?.value || '—';
    var mfgBalanceText = document.getElementById('supplierMfgBalance')?.value || '—';

    section.innerHTML =
        '<div class="bg-surface-container rounded-lg border border-outline-variant/30 p-4 space-y-3">' +
            '<div class="flex justify-between"><span class="text-secondary">المورد</span><span class="font-semibold text-on-surface">' + escapeHtml(supplierName) + '</span></div>' +
            '<div class="flex justify-between"><span class="text-secondary">رصيد الذهب الحالي</span><span class="font-data-mono text-on-surface" dir="ltr">' + escapeHtml(goldBalanceText) + '</span></div>' +
            '<div class="flex justify-between"><span class="text-secondary">أجور التصنيع المستحقة</span><span class="font-data-mono text-on-surface" dir="ltr">' + escapeHtml(mfgBalanceText) + '</span></div>' +
        '</div>' +

        '<div class="bg-surface-container rounded-lg border border-outline-variant/30 p-4">' +
            '<h4 class="font-semibold text-on-surface mb-2">تفاصيل الذهب</h4>' +
            '<table class="w-full text-sm">' +
                '<thead><tr class="border-b border-outline-variant/30"><th class="py-2 px-3 text-right text-secondary">العيار</th><th class="py-2 px-3 text-right text-secondary">الوزن الفعلي</th><th class="py-2 px-3 text-right text-secondary">المكافئ (21)</th></tr></thead>' +
                '<tbody>' + linesHtml + '</tbody>' +
                '<tfoot><tr class="border-t border-outline-variant font-semibold"><td class="py-2 px-3">الإجمالي</td><td class="py-2 px-3"></td><td class="py-2 px-3 font-data-mono text-left dir-ltr">' + totalEquiv21.toFixed(3) + ' جم</td></tr></tfoot>' +
            '</table>' +
        '</div>' +

        '<div class="bg-surface-container rounded-lg border border-outline-variant/30 p-4 space-y-3">' +
            '<div class="flex justify-between"><span class="text-secondary">أجور التصنيع/جرام</span><span class="font-data-mono text-on-surface" dir="ltr">' + feePerGram.toFixed(2) + '</span></div>' +
            '<div class="flex justify-between"><span class="text-secondary">العملة</span><span class="text-on-surface">' + escapeHtml(currencyText) + '</span></div>' +
            '<div class="flex justify-between border-t border-outline-variant/30 pt-3 font-semibold"><span class="text-on-surface">إجمالي أجور التصنيع</span><span class="font-data-mono text-on-surface" dir="ltr">' + totalMfg + '</span></div>' +
        '</div>';
}

// ── Submit ────────────────────────────────────────────
function submitDelivery() {
    var supplierId = document.getElementById('supplierSelect')?.value;
    var feePerGram = parseFloat(document.getElementById('mfgFeePerGram')?.value) || 0;
    var currency = document.getElementById('mfgCurrency')?.value || 'JOD';
    var notes = document.getElementById('deliveryNotes')?.value || '';

    var validLines = goldLines
        .filter(function (l) { return l.weight > 0; })
        .map(function (l) { return { karat: l.karat, weightInGrams: l.weight }; });

    if (!supplierId || validLines.length === 0) {
        showToastMessage('يرجى تعبئة جميع الحقول المطلوبة', 'error', 'توريد المورد');
        return;
    }

    var payload = {
        supplierId: supplierId,
        lines: validLines,
        manufacturingFeePerGram: feePerGram,
        manufacturingFeeCurrency: currency,
        notes: notes
    };

    var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    $.ajax({
        url: '/SupplierDeliveries/CreateDeliveryAjax',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        headers: { 'RequestVerificationToken': token },
        success: function (json) {
            if (typeof json === 'object' && json.status !== undefined) {
                renderToastFromController(json);
            }
        },
        error: function (xhr) {
            if (xhr.status === 400 && xhr.responseJSON) {
                var errorData = xhr.responseJSON;
                if (errorData.errors) {
                    var messages = [];
                    for (var key in errorData.errors) {
                        if (errorData.errors.hasOwnProperty(key)) {
                            errorData.errors[key].forEach(function (msg) {
                                messages.push(msg);
                            });
                        }
                    }
                    showToastMessage(messages.join(' • '), 'error', 'توريد المورد');
                } else if (errorData.title) {
                    showToastMessage(errorData.title, 'error', 'توريد المورد');
                } else if (errorData.detail) {
                    showToastMessage(errorData.detail, 'error', 'توريد المورد');
                } else {
                    showToastMessage('حدث خطأ في التحقق من البيانات', 'error', 'توريد المورد');
                }
            } else {
                showToastMessage('حدث خطأ أثناء حفظ التوريد', 'error', 'توريد المورد');
            }
        }
    });
}

function refreshDeliveryPage() {
    window.location.href = '/SupplierDeliveries';
}

// ── Utility ──────────────────────────────────────────
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