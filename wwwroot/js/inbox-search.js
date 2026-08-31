(function () {
    'use strict';

    var SEARCH_FIELDS = [0, 1, 2, 3];
    var tbody = document.getElementById('inboxRows');
    var input = document.getElementById('inboxSearch');
    if (!tbody || !input) { return; }

    function normalize(text) {
        return (text || '').toString().toLowerCase();
    }

    function applyFilter() {
        var query = normalize(input.value).trim();
        var rows = tbody.querySelectorAll('tr[data-inbox-row]');
        var visibleCount = 0;
        rows.forEach(function (row) {
            if (!query) {
                row.hidden = false;
                visibleCount++;
                return;
            }
            var match = SEARCH_FIELDS.some(function (idx) {
                var cell = row.children[idx];
                return cell && normalize(cell.textContent).indexOf(query) !== -1;
            });
            row.hidden = !match;
            if (match) { visibleCount++; }
        });

        var emptyEl = tbody.querySelector('tr[data-empty-inbox-row]');
        if (emptyEl) {
            emptyEl.hidden = rows.length > 0 && visibleCount > 0;
        }
    }

    input.addEventListener('input', applyFilter);
    document.addEventListener('inbox:refreshed', applyFilter);
    applyFilter();
})();
