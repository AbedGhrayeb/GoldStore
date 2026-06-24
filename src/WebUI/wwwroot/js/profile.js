/* ─── Profile JS ───────────────────────────── */

document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('profileForm');
    if (form) {
        form.addEventListener('submit', handleProfileSubmit);
    }
});

function clearValidationErrors() {
    document.querySelectorAll('#profileForm .border-error').forEach(el => {
        el.classList.remove('border-error');
    });
    document.querySelectorAll('#profileForm [id$="Error"]').forEach(el => {
        el.classList.add('hidden');
        el.textContent = '';
    });
    document.getElementById('editError')?.classList.add('hidden');
}

function showFieldError(fieldId, message) {
    const input = document.getElementById(fieldId);
    const error = document.getElementById(fieldId + 'Error');
    if (input) input.classList.add('border-error');
    if (error) {
        error.textContent = message;
        error.classList.remove('hidden');
    }
}

function handleProfileSubmit(event) {
    event.preventDefault();
    const form = event.target;
    const formData = new FormData(form);
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    clearValidationErrors();

    fetch('/Users/UpdateProfile', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': token,
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json'
        },
        body: formData
    }).then(response => response.json())
      .then(data => {
        if (data.success) {
            toastr.success('تم تحديث بياناتك بنجاح', 'الملف الشخصي');
            setTimeout(() => window.location.reload(), 1500);
        } else {
            if (data.errors) {
                for (const [field, messages] of Object.entries(data.errors)) {
                    if (Array.isArray(messages)) {
                        const fieldKey = field.charAt(0).toLowerCase() + field.slice(1);
                        messages.forEach(msg => showFieldError('edit' + field, msg));
                    }
                }
            }
            if (data.error) {
                document.getElementById('editErrorMessage').textContent = data.error;
                document.getElementById('editError').classList.remove('hidden');
            }
        }
    }).catch(() => {
        document.getElementById('editErrorMessage').textContent = 'حدث خطأ أثناء الاتصال بالخادم';
        document.getElementById('editError').classList.remove('hidden');
    });

    return false;
}

function resetForm() {
    window.location.reload();
}
