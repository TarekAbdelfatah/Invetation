(function () {
    'use strict';

    const OVERLAY_ID = 'bogLoadingOverlay';
    const SHOW_DELAY_MS = 120;
    const HIDE_DELAY_MS = 0;
    const MAX_AUTO_HIDE_MS = 30000;

    function getOverlay() {
        let el = document.getElementById(OVERLAY_ID);
        if (!el) {
            const tpl = document.createElement('div');
            tpl.innerHTML = `
<div class="bog-loading-overlay" id="${OVERLAY_ID}" hidden aria-live="polite" aria-busy="true" role="status">
    <div class="bog-loading-card">
        <div class="bog-cube" aria-hidden="true">
            <i style="background:#c69963"></i>
            <i style="background:#079455"></i>
            <i style="background:#0d6efd"></i>
            <i style="background:#D92D20"></i>
            <i style="background:#7c3aed"></i>
            <i style="background:#f59e0b"></i>
            <i style="background:#c69963"></i>
            <i style="background:#079455"></i>
            <i style="background:#0d6efd"></i>
        </div>
        <p class="bog-loading-text">
            <span class="bog-loading-title">جارٍ المعالجة</span>
            <span class="bog-loading-dots" aria-hidden="true"><i></i><i></i><i></i></span>
        </p>
    </div>
</div>`;
            el = tpl.firstElementChild;
            document.body.appendChild(el);
        }
        return el;
    }

    let hideTimer = null;
    let showTimer = null;
    let safetyTimer = null;

    function show(text) {
        if (showTimer) clearTimeout(showTimer);
        showTimer = setTimeout(() => {
            const el = getOverlay();
            const titleEl = el.querySelector('.bog-loading-title');
            if (text && titleEl) titleEl.textContent = text;
            el.hidden = false;
            el.classList.add('is-shown');
            el.setAttribute('aria-busy', 'true');
            if (safetyTimer) clearTimeout(safetyTimer);
            safetyTimer = setTimeout(() => hide(), MAX_AUTO_HIDE_MS);
        }, SHOW_DELAY_MS);
    }

    function hide() {
        if (showTimer) { clearTimeout(showTimer); showTimer = null; }
        if (safetyTimer) { clearTimeout(safetyTimer); safetyTimer = null; }
        const el = document.getElementById(OVERLAY_ID);
        if (!el) return;
        el.classList.remove('is-shown');
        el.setAttribute('aria-busy', 'false');
        setTimeout(() => {
            if (el && !el.classList.contains('is-shown')) el.hidden = true;
        }, 250);
    }

    function shouldShowForForm(form) {
        if (!form) return false;
        if (form.dataset && form.dataset.loading === 'false') return false;
        if (form.dataset && form.dataset.loading === 'true') return true;
        const submitter = arguments[1];
        if (submitter && submitter.dataset && submitter.dataset.loading === 'true') return true;
        return true;
    }

    function attachFormListeners() {
        document.addEventListener('submit', (e) => {
            const form = e.target;
            if (!(form instanceof HTMLFormElement)) return;
            if (!form.method || form.method.toLowerCase() !== 'post') return;
            const submitter = e.submitter;
            if (!shouldShowForForm(form, submitter)) return;

            if (typeof form.checkValidity === 'function' && !form.checkValidity()) return;

            const explicitText = (submitter && submitter.dataset && submitter.dataset.loadingText) || null;
            show(explicitText || 'جارٍ المعالجة');
        }, true);

        window.addEventListener('pageshow', () => hide());
        window.addEventListener('pagehide', () => hide());
    }

    function attachXhrListeners() {
        const origOpen = XMLHttpRequest.prototype.open;
        XMLHttpRequest.prototype.open = function () {
            const xhr = this;
            let watched = false;
            const wrap = (cb) => function () {
                if (!watched) return cb.apply(this, arguments);
                try { cb.apply(this, arguments); }
                finally { hide(); }
            };
            const origSend = xhr.send;
            xhr.send = function (body) {
                try {
                    show('جارٍ المعالجة');
                } catch (_) {}
                watched = true;
                xhr.addEventListener('loadend', () => hide(), { once: true });
                return origSend.apply(this, arguments);
            };
            return origOpen.apply(this, arguments);
        };
    }

    function attachFetchListeners() {
        if (!window.fetch) return;
        const origFetch = window.fetch.bind(window);
        window.fetch = function (input, init) {
            const method = (init && init.method) || (input instanceof Request && input.method) || 'GET';
            if (typeof method === 'string' && method.toUpperCase() === 'POST') {
                show('جارٍ المعالجة');
                const cleanup = () => hide();
                return origFetch(input, init).then((r) => { cleanup(); return r; }, (e) => { cleanup(); throw e; });
            }
            return origFetch(input, init);
        };
    }

    document.addEventListener('DOMContentLoaded', () => {
        attachFormListeners();
        attachXhrListeners();
        attachFetchListeners();
    });

    window.BogLoading = { show, hide };
})();
