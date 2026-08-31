// Decorates each [data-idea-progress] element on My Requests rows with
// aria-current on the active stage and a tooltip on the track.
// Server-side already computes stage + label; this only polishes accessibility.

(function () {
    function decorate(root) {
        var stage = parseInt(root.getAttribute('data-stage') || '1', 10);
        var label = root.getAttribute('data-stage-label') || '';
        var total = parseInt(root.getAttribute('data-total-stages') || '5', 10);

        var active = root.querySelector('.idea-progress__step[data-step="' + stage + '"]');
        if (active) active.setAttribute('aria-current', 'step');

        if (label) {
            root.setAttribute('aria-label',
                (root.getAttribute('aria-label') || 'مرحلة الفكرة') + ' — ' + label);
        }
    }

    function bindIdeaProgress() {
        var nodes = document.querySelectorAll('[data-idea-progress]');
        nodes.forEach(decorate);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bindIdeaProgress);
    } else {
        bindIdeaProgress();
    }
})();
