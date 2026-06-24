/* ═══════════════════════════════════════════════════════
   SITE.JS — Global Infrastructure
   Toastr config, AJAX modal, AJAX form, toast rendering
   ═══════════════════════════════════════════════════════ */

// ── Toastr Configuration ─────────────────────────────
toastr.options = {
    closeButton: true,
    debug: false,
    newestOnTop: true,
    progressBar: true,
    positionClass: "toast-bottom-left",
    preventDuplicates: true,
    onclick: null,
    showDuration: "300",
    hideDuration: "1000",
    timeOut: "5000",
    extendedTimeOut: "1000",
    showEasing: "swing",
    hideEasing: "linear",
    showMethod: "fadeIn",
    hideMethod: "fadeOut",
    rtl: true
};

// ── Show Toast Message ───────────────────────────────
function showToastMessage(msg, color, title) {
    if (msg instanceof Array) {
        var errorsHtml = "<ul class='list-none pr-0 mr-0'>";
        msg.forEach(function (item) {
            errorsHtml += '<li>- ' + item + '</li>';
        });
        errorsHtml += "</ul>";
        msg = errorsHtml;
    }
    title = title || '';
    if (color === 'success') {
        toastr.success(msg, title);
    } else if (color === 'error') {
        toastr.error(msg, title);
    } else if (color === 'warning') {
        toastr.warning(msg, title);
    } else {
        toastr.info(msg, title);
    }
}

// ── Render Toast From Controller ─────────────────────
function renderToastFromController(json) {
    switch (json.status) {
        case 1: // Success
            showToastMessage(json.msg, json.color, json.management);
            if (json.link && json.link !== '') {
                setTimeout(function () { window.location.replace(json.link); }, 800);
            } else {
                var form = document.querySelector('.ajaxForm');
                if (form) form.reset();
                closeModal();
            }
            break;
        case 0: // Error
            showToastMessage(json.msg, json.color, json.management);
            break;
        case -1: // Exist
            if (json.link && json.link !== '') {
                window.location.replace(json.link);
            }
            var existMsg = json.msg;
            if (json.msg instanceof Array) {
                existMsg = json.msg.join(' • ');
            }
            showToastMessage(existMsg, json.color, json.management);
            break;
        case 2: // Warning
            showToastMessage(json.msg, json.color, json.management);
            break;
        default:
            showToastMessage(json.msg, json.color, json.management);
            break;
    }
    if (json.callback) {
        var callback = window[json.callback];
        if (typeof callback === 'function') {
            callback();
        }
    }
}

// ── Shared Modal (Tailwind-based, no Bootstrap) ──────
function openSharedModal() {
    var modal = document.getElementById('openModal');
    if (modal) {
        modal.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }
}

function closeModal() {
    var modal = document.getElementById('openModal');
    if (modal) {
        modal.classList.add('hidden');
        document.body.style.overflow = '';
        var content = document.getElementById('openModalContent');
        if (content) content.innerHTML = '';
    }
}

// ── Open Modal (via AJAX) ────────────────────────────
function getModalContent(sourceUrl, ajaxType, modalWrapper, title) {
    var contentEl = document.getElementById('openModalContent');
    if (!contentEl) return;

    if (sourceUrl && sourceUrl !== '#') {
        $.ajax({
            method: ajaxType || 'GET',
            url: sourceUrl,
            contentType: 'application/json'
        }).done(function (result) {
            if (typeof result === 'object' && result.status !== undefined) {
                if (result.status === 0 || result.status === -1) {
                    showToastMessage(result.msg, result.color, result.management);
                    return;
                }
            }
            contentEl.innerHTML = result;
            loadAjaxFormForPartialView();
            openSharedModal();
        }).fail(function (res) {
            if (res.status === 403) {
                showToastMessage('ليس لديك صلاحية الوصول', 'error', 'خطأ');
            } else {
                showToastMessage('حدث خطأ أثناء التحميل', 'error', 'خطأ');
            }
        });
    }
}

// ── Load AJAX Form for PartialView ───────────────────
function loadAjaxFormForPartialView() {
    var $form = $('.ajaxForm');

    // Re-parse unobtrusive validation for new form elements
    if ($form.length && $.validator && $.validator.unobtrusive) {
        $form.each(function () {
            if (!$(this).data('validator')) {
                $.validator.unobtrusive.parse($(this));
            }
        });
    }

    // Remove any previous ajaxForm binding to avoid duplicates
    $form.off('submit.ajaxForm');

    $form.ajaxForm({
        success: function (json) {
            $('.ajaxForm :submit').prop('disabled', false).removeClass('m-loader m-loader--light m-loader--right');
            if (typeof json === 'object' && json.status !== undefined) {
                renderToastFromController(json);
            } else {
                closeModal();
            }
        },
        beforeSubmit: function () {
            $('.ajaxForm :submit').prop('disabled', true).addClass('m-loader m-loader--light m-loader--right');
            $('.ajaxForm .is-invalid').removeClass('is-invalid');
            $('.ajaxForm .invalid-feedback').text('');
        },
        error: function (xhr) {
            $('.ajaxForm :submit').prop('disabled', false).removeClass('m-loader m-loader--light m-loader--right');
            if (xhr.status === 400 && xhr.responseJSON && xhr.responseJSON.errors) {
                var errors = xhr.responseJSON.errors;
                for (var key in errors) {
                    if (errors.hasOwnProperty(key)) {
                        var input = $('[name="' + key + '"]');
                        input.addClass('is-invalid');
                        var feedback = input.siblings('.invalid-feedback');
                        if (feedback.length && errors[key].length > 0) {
                            feedback.text(errors[key][0]);
                        }
                    }
                }
            } else {
                showToastMessage('حدث خطأ أثناء الاتصال بالخادم', 'error', 'خطأ');
            }
        }
    });
}

// ── .openModal click handler ─────────────────────────
$(document).on('click', '.openModal', function (e) {
    e.preventDefault();
    var href = $(this).attr('href');
    var ajaxType = $(this).attr('ajaxType') || 'GET';
    var title = $(this).attr('title') || '';
    getModalContent(href, ajaxType, '#openModal', title);
});

// ── Remove is-invalid on focus ───────────────────────
$(document).on('focus', '.form-control, input, select, textarea', function () {
    $(this).removeClass('is-invalid');
});

// ── Close modal on overlay click ──────────────────────
$(document).on('click', '#openModalOverlay', function () {
    closeModal();
});

// ── Close modal on Escape key ─────────────────────────
$(document).on('keydown', function (e) {
    if (e.key === 'Escape') {
        var modal = document.getElementById('openModal');
        if (modal && !modal.classList.contains('hidden')) {
            closeModal();
        }
    }
});