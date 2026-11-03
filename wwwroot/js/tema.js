(() => {
    const root = document.documentElement;
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)');
    let preference;
    try { preference = localStorage.getItem('solargrid.tema'); } catch { /* Storage may be unavailable. */ }
    if (preference !== 'dark' && preference !== 'light') preference = null;

    function applyTheme(theme) {
        root.dataset.tema = theme;
        const button = document.getElementById('selector-tema');
        if (button) {
            const dark = theme === 'dark';
            button.setAttribute('aria-checked', String(dark));
            button.title = dark ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro';
        }
    }

    applyTheme(preference || (systemTheme.matches ? 'dark' : 'light'));
    systemTheme.addEventListener('change', event => {
        if (!preference) applyTheme(event.matches ? 'dark' : 'light');
    });
    window.addEventListener('storage', event => {
        if (event.key !== 'solargrid.tema' && event.key !== null) return;
        preference = event.newValue === 'dark' || event.newValue === 'light' ? event.newValue : null;
        applyTheme(preference || (systemTheme.matches ? 'dark' : 'light'));
    });
    document.addEventListener('DOMContentLoaded', () => {
        applyTheme(root.dataset.tema);
        document.getElementById('selector-tema')?.addEventListener('click', () => {
            preference = root.dataset.tema === 'dark' ? 'light' : 'dark';
            applyTheme(preference);
            try { localStorage.setItem('solargrid.tema', preference); } catch { /* Keep the current page usable. */ }
        });
    });
})();
