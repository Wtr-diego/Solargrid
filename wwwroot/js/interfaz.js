(() => {
    const button = document.querySelector('.boton-menu');
    const menu = document.getElementById('menu-principal');
    if (!button || !menu) return;
    function close() {
        document.body.classList.remove('menu-abierto');
        button.setAttribute('aria-expanded', 'false');
        button.setAttribute('aria-label', 'Abrir menú de navegación');
    }
    button.addEventListener('click', () => {
        const open = document.body.classList.toggle('menu-abierto');
        button.setAttribute('aria-expanded', String(open));
        button.setAttribute('aria-label', open ? 'Cerrar menú de navegación' : 'Abrir menú de navegación');
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && document.body.classList.contains('menu-abierto')) { close(); button.focus(); }
    });
    document.addEventListener('click', event => {
        if (!menu.contains(event.target) && !button.contains(event.target)) close();
    });
    window.matchMedia('(min-width: 901px)').addEventListener('change', close);
})();
