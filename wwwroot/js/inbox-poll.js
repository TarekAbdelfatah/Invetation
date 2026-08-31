(function () {
    'use strict';

    var POLL_INTERVAL_MS = 5000;
    var tbody = document.getElementById('inboxRows');
    if (!tbody) { return; }

    var applicantType = tbody.getAttribute('data-applicant-type') || '';
    var status = tbody.getAttribute('data-status') || '';

    function buildUrl() {
        var params = new URLSearchParams();
        if (applicantType) params.set('applicantType', applicantType);
        if (status) params.set('status', status);
        var qs = params.toString();
        return '/Audit/InboxRows' + (qs ? '?' + qs : '');
    }

    async function refresh() {
        try {
            var res = await fetch(buildUrl(), {
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (!res.ok) { return; }
            var html = await res.text();
            tbody.innerHTML = html;
        } catch (err) {
            console.warn('Inbox poll failed', err);
        }
    }

    setInterval(refresh, POLL_INTERVAL_MS);
})();
