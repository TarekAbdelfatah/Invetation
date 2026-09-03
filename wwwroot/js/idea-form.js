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

        function refresh() {
            techPanel.classList.toggle('d-none', !toggle.checked);
        }

        toggle.addEventListener('change', refresh);
        refresh();
    }

    function initTechOtherField() {
        var techList = document.querySelector('[data-tech-list="true"]');
        var otherField = document.querySelector('[data-other-field="TechnologyOther"]');
        if (!techList || !otherField) { return; }

        var checkboxes = techList.querySelectorAll('input[type="checkbox"]');
        function refresh() {
            var isOtherChecked = false;
            for (var i = 0; i < checkboxes.length; i++) {
                var label = checkboxes[i].nextElementSibling;
                if (label && label.textContent.indexOf('أخرى') !== -1 && checkboxes[i].checked) {
                    isOtherChecked = true;
                    break;
                }
            }
            otherField.classList.toggle('d-none', !isOtherChecked);
            if (!isOtherChecked) {
                var input = otherField.querySelector('input');
                if (input) { input.value = ''; }
            }
        }

        for (var i = 0; i < checkboxes.length; i++) {
            checkboxes[i].addEventListener('change', refresh);
        }
        refresh();
    }

    function bootstrap() {
        initCounters();
        initOtherFields();
        initEmergingTech();
        initTechOtherField();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }
})();
