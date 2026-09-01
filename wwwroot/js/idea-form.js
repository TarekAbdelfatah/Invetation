(function () {
    'use strict';

    function decorateCounter(textarea) {
        var limit = parseInt(textarea.getAttribute('data-char-limit'), 10);
        if (!limit || limit <= 0) { return; }

        var name = textarea.getAttribute('name');
        var counter = document.querySelector('[data-char-counter="' + name + '"]');
        if (!counter) { return; }

        function update() {
            var length = textarea.value.length;
            var remaining = Math.Max ? Math.max(0, limit - length) : Math.max(0, limit - length);
            if (textarea.value.length > limit) {
                textarea.value = textarea.value.slice(0, limit);
                remaining = 0;
            }
            counter.textContent = remaining + ' حرف متبقٍ من ' + limit;
            counter.classList.toggle('text-danger', remaining <= 0);
        }

        textarea.addEventListener('input', update);
        update();
    }

    function refreshOther(select) {
        var otherName = select.getAttribute('data-other');
        if (!otherName) { return; }
        var otherField = document.querySelector('[data-other-field="' + otherName + '"]');
        if (!otherField) { return; }

        var selectedText = select.options[select.selectedIndex].text;
        var isOther = selectedText.indexOf('أخرى') !== -1;
        otherField.classList.toggle('d-none', !isOther);
        if (!isOther) {
            var input = otherField.querySelector('input');
            if (input) { input.value = ''; }
        }
    }

    function initOtherFields() {
        var selects = document.querySelectorAll('select[data-other]');
        for (var i = 0; i < selects.length; i++) {
            (function (select) {
                select.addEventListener('change', function () { refreshOther(select); });
                refreshOther(select);
            })(selects[i]);
        }
    }

    function initCounters() {
        var textareas = document.querySelectorAll('textarea[data-char-limit]');
        for (var i = 0; i < textareas.length; i++) {
            decorateCounter(textareas[i]);
        }
    }

    function initEmergingTech() {
        var toggle = document.getElementById('uses-emerging-tech');
        var techPanel = document.querySelector('[data-emerging-tech="true"]');
        if (!toggle || !techPanel) { return; }

        var techList = document.querySelector('[data-tech-list="true"]');
        function refresh() {
            techPanel.classList.toggle('d-none', !toggle.checked);
        }

        toggle.addEventListener('change', refresh);
        refresh();
    }

    function fieldLabel(form, name) {
        var label = form.querySelector('label[for="' + name + '"], label.asp-for-' + name);
        if (label) {
            var star = label.querySelector('.text-danger');
            return (label.textContent.replace('*', '').trim() || label.textContent.trim());
        }
        var field = form.querySelector('[name="' + name + '"]');
        if (field && field.id) {
            var l2 = document.querySelector('label[for="' + field.id + '"]');
            if (l2) return l2.textContent.replace('*', '').trim() || l2.textContent.trim();
        }
        return name;
    }

    function isFieldEmpty(input) {
        if (!input) return true;
        var tag = (input.tagName || '').toLowerCase();
        if (tag === 'select') {
            var val = input.value;
            return !val || val === '' || val === '00000000-0000-0000-0000-000000000000';
        }
        if (tag === 'textarea' || tag === 'input') {
            var v = (input.value || '').trim();
            return v.length === 0;
        }
        return !input.value;
    }

    function clearErrorFor(input) {
        input.classList.remove('is-invalid');
        var msg = document.querySelector('.bog-error-msg[data-for="' + input.name + '"]');
        if (msg) msg.remove();
        if (input.setCustomValidity) input.setCustomValidity('');
    }

    function showErrorFor(input, message) {
        input.classList.add('is-invalid');
        var existing = document.querySelector('.bog-error-msg[data-for="' + input.name + '"]');
        if (existing) { existing.textContent = message; return; }
        var span = document.createElement('span');
        span.className = 'bog-error-msg text-danger small d-block mt-1';
        span.setAttribute('data-for', input.name);
        span.textContent = message;
        input.parentNode.appendChild(span);
        if (input.setCustomValidity) input.setCustomValidity(message);
    }

    function initSubmitMode() {
        var form = document.getElementById('idea-form');
        if (!form) { return; }

        var submitMode = 'draft';
        var submitBtn = form.querySelector('button[value="Submit"]');
        var draftBtn = form.querySelector('button[value="SaveDraft"]');
        if (!submitBtn || !draftBtn) { return; }

        var summaryEl = document.getElementById('ideaFormSummary');

        function clearAllSubmitErrors() {
            var msgs = form.querySelectorAll('.bog-error-msg');
            for (var i = 0; i < msgs.length; i++) msgs[i].remove();
            var invalids = form.querySelectorAll('.is-invalid');
            for (var j = 0; j < invalids.length; j++) invalids[j].classList.remove('is-invalid');
            if (summaryEl) summaryEl.classList.add('d-none');
        }

        function attachLiveClears() {
            form.addEventListener('input', function (e) {
                if (e.target instanceof HTMLElement) clearErrorFor(e.target);
            }, true);
            form.addEventListener('change', function (e) {
                if (e.target instanceof HTMLElement) clearErrorFor(e.target);
            }, true);
        }

        function setRequired(mode) {
            var inputs = form.querySelectorAll('[data-required-on-submit]');
            for (var i = 0; i < inputs.length; i++) {
                if (mode === 'submit') {
                    inputs[i].setAttribute('required', 'required');
                } else {
                    inputs[i].removeAttribute('required');
                    inputs[i].setCustomValidity('');
                }
            }
        }

        submitBtn.addEventListener('click', function () { submitMode = 'submit'; });
        draftBtn.addEventListener('click', function () {
            submitMode = 'draft';
            clearAllSubmitErrors();
        });

        attachLiveClears();

        form.addEventListener('submit', function (e) {
            setRequired(submitMode);
            if (submitMode !== 'submit') return;

            var empties = [];
            var requiredFields = form.querySelectorAll('[required]');
            for (var i = 0; i < requiredFields.length; i++) {
                var input = requiredFields[i];
                if (isFieldEmpty(input)) {
                    var name = input.getAttribute('name') || input.id || ('field-' + i);
                    empties.push({ input: input, name: name });
                }
            }

            if (empties.length === 0) return;

            e.preventDefault();
            clearAllSubmitErrors();

            try {
                if (window.BogLoading && typeof window.BogLoading.cancel === 'function') {
                    window.BogLoading.cancel();
                }
            } catch (_) { /* ignore */ }

            var names = [];
            for (var k = 0; k < empties.length; k++) {
                var it = empties[k];
                var label = fieldLabel(form, it.name) || it.name;
                showErrorFor(it.input, label + ' مطلوب');
                names.push(label);
            }

            if (summaryEl) {
                summaryEl.innerHTML = '<strong>يرجى تعبئة الحقول المطلوبة:</strong> ' + names.join('، ');
                summaryEl.classList.remove('d-none');
            }

            (empties[0] && empties[0].input && empties[0].input.focus && empties[0].input.focus());
            try { window.scrollTo({ top: form.offsetTop - 80, behavior: 'smooth' }); } catch (_) {}
        });
    }

    function bootstrap() {
        initCounters();
        initOtherFields();
        initEmergingTech();
        initSubmitMode();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }
})();
