// Client-side helpers for the Ibtikar portal.
// Keeps each concern in a small function bound on DOMContentLoaded.

(function () {

    // ---------- Navbar collapse ----------
    function bindNavbar() {
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-nav-toggle]');
            if (!btn) return;
            var target = document.querySelector(btn.getAttribute('data-nav-toggle'));
            if (!target) return;
            var open = target.classList.toggle('show');
            btn.setAttribute('aria-expanded', String(open));
        });
    }

    // ---------- Confirm modal ----------
    function bindConfirmModal() {
        var modal = document.getElementById('confirm-modal');
        if (!modal) return;

        var messageEl = modal.querySelector('[data-confirm-message]');
        var okBtn = modal.querySelector('[data-confirm-ok]');
        var cancelBtns = modal.querySelectorAll('[data-confirm-cancel]');
        var lastTrigger = null;
        var pendingForm = null;

        function close() {
            modal.hidden = true;
            modal.removeAttribute('data-open');
            if (lastTrigger) { lastTrigger.focus(); lastTrigger = null; }
            pendingForm = null;
        }

        function open(trigger) {
            lastTrigger = trigger;
            var msg = trigger.getAttribute('data-confirm-message')
                || trigger.getAttribute('data-confirm')
                || messageEl.textContent;
            messageEl.textContent = msg;
            var okLabel = trigger.getAttribute('data-confirm-ok');
            if (okLabel) okBtn.textContent = okLabel;

            var formSelector = trigger.getAttribute('data-confirm-form');
            pendingForm = formSelector ? document.querySelector(formSelector)
                : trigger.closest('form');

            if (pendingForm && trigger.name) {
                var hidden = pendingForm.querySelector('input[type="hidden"][name="' + trigger.name + '"]');
                if (hidden) hidden.value = trigger.value || '';
            }

            modal.hidden = false;
            modal.setAttribute('data-open', 'true');
            okBtn.focus();
        }

        document.addEventListener('click', function (e) {
            var trigger = e.target.closest('[data-confirm-trigger]');
            if (trigger) {
                if (trigger.tagName === 'BUTTON' && trigger.type === 'submit' && trigger.form) {
                    pendingForm = trigger.form;
                }
                e.preventDefault();
                open(trigger);
                return;
            }
            if (e.target.closest('[data-confirm-cancel]')) { close(); return; }
            if (e.target.closest('[data-confirm-ok]')) {
                if (pendingForm) pendingForm.submit();
                close();
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !modal.hidden) close();
        });
    }

    // ---------- Toast / alert auto-dismiss ----------
    function bindToasts() {
        document.querySelectorAll('[data-toast]').forEach(function (el) {
            var ttl = parseInt(el.getAttribute('data-toast-ttl') || '4000', 10);
            if (el.querySelector('[data-toast-close]')) {
                el.querySelector('[data-toast-close]').addEventListener('click', function () {
                    el.hidden = true;
                });
            }
            if (ttl > 0) setTimeout(function () { el.hidden = true; }, ttl);
        });
    }

    // ---------- Related field toggle ----------
    // Usage:
    //   <select data-related-toggle="#field">
    //   <input id="field" data-related-show-when="value1,value2">
    function bindRelatedToggle() {
        document.querySelectorAll('[data-related-toggle]').forEach(function (ctrl) {
            var target = document.querySelector(ctrl.getAttribute('data-related-toggle'));
            if (!target) return;
            var values = (target.getAttribute('data-related-show-when') || '')
                .split(',').map(function (v) { return v.trim(); });
            function sync() {
                var show = values.length === 0 || values.indexOf(ctrl.value) !== -1;
                target.hidden = !show;
            }
            ctrl.addEventListener('change', sync);
            sync();
        });
    }

    // ---------- Character counter ----------
    // Usage:
    //   <textarea maxlength="500" data-counter="#counter"></textarea>
    //   <span id="counter"></span>
    function bindCounters() {
        document.querySelectorAll('[data-counter]').forEach(function (src) {
            var target = document.querySelector(src.getAttribute('data-counter'));
            if (!target) return;
            var max = parseInt(src.getAttribute('maxlength') || '0', 10);
            function sync() {
                var len = src.value.length;
                target.textContent = max > 0 ? (len + ' / ' + max) : String(len);
            }
            src.addEventListener('input', sync);
            sync();
        });
    }

    function bootstrap() {
        bindNavbar();
        bindConfirmModal();
        bindToasts();
        bindRelatedToggle();
        bindCounters();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }
})();
