// Theme preference, reading progress, return-to-top, and live project snapshots.
(function () {
    'use strict';
    var STORAGE_KEY = 'theme';
    var media = window.matchMedia('(prefers-color-scheme: dark)');

    function readPreference() {
        try { return localStorage.getItem(STORAGE_KEY) || 'system'; }
        catch (e) { return 'system'; }
    }
    function savePreference(value) {
        try { localStorage.setItem(STORAGE_KEY, value); } catch (e) { }
    }
    function applyTheme(pref) {
        var dark = pref === 'dark' || (pref === 'system' && media.matches);
        var theme = dark ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-bs-theme', theme);
    }
    function setAllSelects(value) {
        document.querySelectorAll('[data-theme-select]').forEach(function (select) { select.value = value; });
    }

    function initTheme() {
        var preference = readPreference();
        setAllSelects(preference);
        applyTheme(preference);
        document.querySelectorAll('[data-theme-select]').forEach(function (select) {
            select.addEventListener('change', function (event) {
                var value = event.target.value;
                savePreference(value);
                applyTheme(value);
                setAllSelects(value);
            });
        });
        var onSystemChange = function () { if (readPreference() === 'system') { applyTheme('system'); } };
        if (media.addEventListener) { media.addEventListener('change', onSystemChange); }
        else if (media.addListener) { media.addListener(onSystemChange); }
    }

    function initScrollTools() {
        var progress = document.createElement('div');
        progress.className = 'scroll-progress';
        progress.setAttribute('role', 'progressbar');
        progress.setAttribute('aria-label', 'Page reading progress');
        progress.setAttribute('aria-valuemin', '0');
        progress.setAttribute('aria-valuemax', '100');
        var fill = document.createElement('span');
        progress.appendChild(fill);
        document.body.appendChild(progress);

        var topButton = document.createElement('button');
        topButton.type = 'button';
        topButton.className = 'back-to-top';
        topButton.setAttribute('aria-label', 'Back to top');
        topButton.innerHTML = '<i class="bi bi-arrow-up" aria-hidden="true"></i>';
        document.body.appendChild(topButton);
        topButton.addEventListener('click', function () { window.scrollTo({ top: 0, behavior: 'smooth' }); });

        var pending = false;
        function update() {
            var max = document.documentElement.scrollHeight - window.innerHeight;
            var percent = max > 0 ? Math.min(100, Math.max(0, window.scrollY / max * 100)) : 0;
            fill.style.width = percent + '%';
            progress.setAttribute('aria-valuenow', String(Math.round(percent)));
            topButton.classList.toggle('visible', window.scrollY > 360);
            pending = false;
        }
        function scheduleUpdate() {
            if (!pending) { pending = true; window.requestAnimationFrame(update); }
        }
        window.addEventListener('scroll', scheduleUpdate, { passive: true });
        window.addEventListener('resize', scheduleUpdate, { passive: true });
        update();
    }

    function initRevealMotion() {
        var candidates = document.querySelectorAll('.section, .stat-tile, .how-card, .feature-card, .verdict-card, .step-card, .match-card, .featured-card, .next-steps, .role-card, .auth-card, .table-card, .proj-card, .filter-panel, .detail-panel, .cta-band');
        candidates.forEach(function (el, index) {
            if (!el.hasAttribute('data-reveal')) {
                el.setAttribute('data-reveal', el.matches('.auth-card,.featured-card') ? 'scale' : '');
                el.style.setProperty('--reveal-delay', Math.min(index % 5, 4) * 65 + 'ms');
            }
        });
        if (!('IntersectionObserver' in window)) { candidates.forEach(function (el) { el.classList.add('is-revealed'); }); return; }
        document.documentElement.classList.add('js-motion');
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) { entry.target.classList.add('is-revealed'); observer.unobserve(entry.target); }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -35px 0px' });
        candidates.forEach(function (el) { observer.observe(el); });
    }

    function initProjectSnapshots() {
        var panel = document.getElementById('projectDetailsPanel');
        if (!panel) {
            document.querySelectorAll('[data-project-quick-view]').forEach(function (button) { button.hidden = true; });
            return;
        }
        var placeholder = document.getElementById('detailPlaceholder');
        var content = document.getElementById('detailContent');
        var title = document.getElementById('detailTitle');
        var university = document.getElementById('detailUniversity');
        var description = document.getElementById('detailDescription');
        var tags = document.getElementById('detailTags');
        var meta = document.getElementById('detailMeta');
        var source = document.getElementById('detailSource');
        var noSource = document.getElementById('detailNoSource');

        function addTag(text) {
            if (!text || !text.trim()) { return; }
            var tag = document.createElement('span');
            tag.textContent = text;
            tags.appendChild(tag);
        }
        function addMeta(icon, label, value) {
            if (!value || !value.trim()) { return; }
            var item = document.createElement('span');
            var glyph = document.createElement('i');
            glyph.className = 'bi ' + icon;
            glyph.setAttribute('aria-hidden', 'true');
            var text = document.createElement('span');
            text.textContent = label + ': ' + value;
            item.appendChild(glyph);
            item.appendChild(text);
            meta.appendChild(item);
        }
        document.addEventListener('click', function (event) {
            var button = event.target.closest('[data-project-quick-view]');
            if (!button) { return; }
            var card = button.closest('[data-project-card]');
            if (!card) { return; }
            document.querySelectorAll('[data-project-card].is-selected').forEach(function (item) {
                item.classList.remove('is-selected');
                var previous = item.querySelector('[data-project-quick-view]');
                if (previous) { previous.setAttribute('aria-pressed', 'false'); }
            });
            card.classList.add('is-selected');
            button.setAttribute('aria-pressed', 'true');
            title.textContent = card.getAttribute('data-title') || 'Untitled project';
            university.textContent = card.getAttribute('data-university') || card.querySelector('.uni-tag')?.textContent.trim() || 'University not specified';
            description.textContent = card.getAttribute('data-description') || 'No project description is available.';
            tags.replaceChildren();
            meta.replaceChildren();
            addTag(card.getAttribute('data-year'));
            addTag(card.getAttribute('data-area'));
            addTag(card.getAttribute('data-award'));
            addTag(card.getAttribute('data-title-only') === 'true' ? 'Title only' : 'Database record');
            addMeta('bi-person', 'Supervisor', card.getAttribute('data-supervisor'));
            addMeta('bi-people', 'Authors', card.getAttribute('data-authors'));
            var url = card.getAttribute('data-source') || '';
            if (/^https?:\/\//i.test(url)) {
                source.href = url;
                source.classList.remove('d-none');
                noSource.classList.add('d-none');
            } else {
                source.removeAttribute('href');
                source.classList.add('d-none');
                noSource.classList.remove('d-none');
            }
            placeholder.classList.add('d-none');
            content.classList.remove('d-none');
            if (window.matchMedia('(max-width: 767.98px)').matches) {
                panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }
        });
    }

    function init() {
        initTheme();
        initScrollTools();
        initRevealMotion();
        initProjectSnapshots();
    }
    if (document.readyState === 'loading') { document.addEventListener('DOMContentLoaded', init); }
    else { init(); }
})();
