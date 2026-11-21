// Theme switcher: System / Light / Dark
// Pehla rang page khulte hi _Layout.cshtml ka chhota script laga deta hai.
// Yeh file dropdown ko chalati hai aur choice yaad rakhti hai.
(function () {
    var STORAGE_KEY = 'theme';   // _Layout.cshtml mein bhi yehi naam hai
    var media = window.matchMedia('(prefers-color-scheme: dark)');

    function readPreference() {
        try {
            return localStorage.getItem(STORAGE_KEY) || 'system';
        } catch (e) {
            return 'system';
        }
    }

    function savePreference(value) {
        try {
            localStorage.setItem(STORAGE_KEY, value);
        } catch (e) { }
    }

    // pref: 'system', 'light' ya 'dark'
    function applyTheme(pref) {
        var dark = pref === 'dark' || (pref === 'system' && media.matches);
        var theme = dark ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-bs-theme', theme);
    }

    function setAllSelects(value) {
        var selects = document.querySelectorAll('[data-theme-select]');
        for (var i = 0; i < selects.length; i++) {
            selects[i].value = value;
        }
    }

    function init() {
        var selects = document.querySelectorAll('[data-theme-select]');
        setAllSelects(readPreference());

        for (var i = 0; i < selects.length; i++) {
            selects[i].addEventListener('change', function (event) {
                var value = event.target.value;
                savePreference(value);
                applyTheme(value);
                setAllSelects(value);
            });
        }

        // "System" chuna ho aur computer ka theme badal jaye to page bhi badal jaye
        var onSystemChange = function () {
            if (readPreference() === 'system') {
                applyTheme('system');
            }
        };

        if (media.addEventListener) {
            media.addEventListener('change', onSystemChange);
        } else if (media.addListener) {
            media.addListener(onSystemChange);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();