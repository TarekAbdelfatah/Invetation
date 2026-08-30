// Mobile navbar toggle: pairs [data-nav-toggle] buttons with their targets.
(function () {
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-nav-toggle]');
        if (!btn) return;
        var target = document.querySelector(btn.getAttribute('data-nav-toggle'));
        if (!target) return;
        var open = target.classList.toggle('show');
        btn.setAttribute('aria-expanded', open);
    });
})();
