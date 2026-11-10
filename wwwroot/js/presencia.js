(() => {
    const listado = document.querySelector('[data-url-presencia]');
    const url = listado?.dataset.urlPresencia ?? document.currentScript?.dataset.urlLatido;
    if (!url) return;

    let consultando = false;
    async function actualizar() {
        if (document.hidden || consultando) return;
        consultando = true;
        try {
            const respuesta = await fetch(url, { cache: 'no-store', redirect: 'error' });
            if (!listado || !respuesta.ok) return;

            const usuariosEnLinea = new Set(await respuesta.json());
            for (const indicador of document.querySelectorAll('[data-presencia-usuario]')) {
                const enLinea = usuariosEnLinea.has(Number(indicador.dataset.presenciaUsuario));
                indicador.textContent = enLinea ? 'En línea' : 'Sin conexión';
                indicador.classList.toggle('estado-correcto', enLinea);
                indicador.classList.toggle('estado-neutral', !enLinea);
            }
        } catch {
            // La siguiente consulta volverá a intentar la actualización.
        } finally {
            consultando = false;
        }
    }

    window.setInterval(actualizar, 30000);
    document.addEventListener('visibilitychange', actualizar);
})();
