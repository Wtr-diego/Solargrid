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
        if (event.key !== 'Escape') return;
        const workspace = document.getElementById('espacio-trabajo');
        if (workspace?.open && workspace.contains(document.activeElement)) {
            workspace.open = false;
            workspace.querySelector('summary').focus();
        } else if (document.body.classList.contains('menu-abierto')) { close(); button.focus(); }
    });
    document.addEventListener('click', event => {
        if (!menu.contains(event.target) && !button.contains(event.target)) close();
    });
    window.matchMedia('(min-width: 901px)').addEventListener('change', close);
})();

// También se ejecuta en el login, donde no existe el menú lateral.
(() => {
    document.querySelectorAll('[data-mostrar-contrasena]').forEach(button => {
        const input = document.getElementById(button.dataset.mostrarContrasena);
        if (!input || input.type !== 'password') return;
        const label = button.querySelector('span');
        function setVisible(visible) {
            input.type = visible ? 'text' : 'password';
            button.setAttribute('aria-pressed', String(visible));
            button.setAttribute('aria-label', visible ? 'Ocultar contraseña' : 'Mostrar contraseña');
            label.textContent = visible ? 'Ocultar' : 'Mostrar';
        }
        button.hidden = false;
        button.addEventListener('click', () => setVisible(input.type === 'password'));
        input.form?.addEventListener('submit', () => setVisible(false));
        input.form?.addEventListener('reset', () => setVisible(false));
        window.addEventListener('pageshow', () => setVisible(false));
    });
})();
