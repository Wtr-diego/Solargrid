(() => {
    'use strict';
    const summary = document.querySelector('[data-simulacion]');
    if (!summary) return;
    const page = document.querySelector('[data-simulacion-pagina]');
    const form = document.getElementById('sim-config-form');
    const buttons = Array.from(document.querySelectorAll('[data-accion]'));
    const errorBox = document.getElementById('sim-error');
    const result = document.getElementById('sim-resultado');
    const formats = new Map();
    let state, busy = false, epoch = 0, formDirty = false, formId, drawingKey;
    let refreshTimer;

    function number(value, digits = 2) {
        if (!formats.has(digits)) formats.set(digits, new Intl.NumberFormat('es', { minimumFractionDigits: digits, maximumFractionDigits: digits }));
        return formats.get(digits).format(Number(value) || 0);
    }
    function text(key, value) {
        document.querySelectorAll(`[data-sim="${key}"]`).forEach(element => { element.textContent = value; });
    }
    function dateLabel(value) {
        const parts = String(value || '').slice(0, 10).split('-');
        return parts.length === 3 ? `${parts[2]}/${parts[1]}/${parts[0]}` : '—';
    }
    function hourLabel(value) { return String(value || '').slice(11, 16) || '00:00'; }
    function updateButtons() {
        const ready = !!state && !busy;
        const running = state?.estado === 'En ejecución';
        const completed = state?.horasCompletadas >= 24;
        buttons.forEach(button => {
            const action = button.dataset.accion;
            button.disabled = !ready || (action === 'Iniciar' && (running || completed)) ||
                (action === 'Pausar' && !running) || (action === 'Avanzar' && (running || completed)) ||
                (action === 'Completar' && completed) || (action === 'Guardar' && (!completed || !!state.idSistemaGuardado));
            if (action === 'Iniciar') button.textContent = state?.estado === 'Pausada' ? 'Continuar' : 'Iniciar';
            if (action === 'Guardar') button.textContent = state?.idSistemaGuardado ? 'Lecturas guardadas' : 'Guardar 24 lecturas';
        });
        if (form) form.querySelectorAll('input:not([type="hidden"]), select, button').forEach(control => { control.disabled = !ready; });
        const csv = document.querySelector('[data-sim-csv]');
        if (csv) {
            const disabled = !ready || !state.lecturas?.length;
            csv.setAttribute('aria-disabled', String(disabled));
            csv.tabIndex = disabled ? -1 : 0;
        }
    }
    function render(next) {
        state = next;
        text('lugar', `${state.lugar.nombre}, El Salvador`);
        text('fecha', dateLabel(state.fecha));
        text('estado', state.estado);
        const badge = summary.querySelector('[data-sim="estado"]');
        badge.className = `chip ${state.estado === 'En ejecución' ? 'estado-correcto' : state.estado === 'Completada' ? 'estado-correcto' : 'estado-neutral'}`;
        ['generacionKwh', 'consumoKwh', 'autoconsumoKwh', 'importacionKwh', 'excedenteKwh'].forEach(key => text(key, number(state[key])));
        text('coberturaPorcentaje', number(state.coberturaPorcentaje, 1));
        text('potenciaInstaladaW', number(state.potenciaInstaladaW, 0));
        text('avance', `${state.horasCompletadas} de ${state.totalHoras} horas`);
        const progress = document.getElementById('sim-progreso');
        progress.max = state.totalHoras;
        progress.value = state.horasCompletadas;
        progress.textContent = `${state.horasCompletadas} de ${state.totalHoras} horas`;
        text('conexion', state.estado === 'En ejecución' ? `Actualización automática · 1 hora simulada cada ${state.intervaloSegundos} segundos.` : 'Datos del simulador conectado · actualización automática.');
        if (page) {
            text('coordenadas', `${number(state.lugar.latitud, 4)}, ${number(state.lugar.longitud, 4)}`);
            const map = document.querySelector('[data-sim-mapa]');
            map.href = `https://www.openstreetmap.org/?mlat=${encodeURIComponent(state.lugar.latitud)}&mlon=${encodeURIComponent(state.lugar.longitud)}#map=12/${encodeURIComponent(state.lugar.latitud)}/${encodeURIComponent(state.lugar.longitud)}`;
            map.hidden = false;
            if (form && formId !== state.id && !formDirty) {
                const config = state.configuracion;
                Object.entries({ Ubicacion: config.ubicacion, Clima: config.clima, PotenciaPanelW: config.potenciaPanelW, CantidadPaneles: config.cantidadPaneles, FactorConsumo: config.factorConsumo }).forEach(([key, value]) => { form.elements.namedItem(key).value = value; });
                formId = state.id;
            }
            const report = document.querySelector('[data-sim-reporte]');
            report.hidden = !state.idSistemaGuardado;
            if (state.idSistemaGuardado) {
                report.href = `${page.dataset.reportesUrl}?${new URLSearchParams({ idSistema: state.idSistemaGuardado, desde: state.fecha.slice(0, 10), hasta: state.fecha.slice(0, 10) })}`;
                text('guardado', `24 lecturas guardadas en el sistema [SIM] #${state.idSistemaGuardado}. Puedes consultarlas en Lecturas y Reportes.`);
            } else text('guardado', state.horasCompletadas === 24 ? 'Día completo. Guarda las lecturas para incorporarlas a la base de datos del proyecto.' : 'Aún no se han guardado lecturas de esta simulación. Completa el día para guardarlas.');
            const key = `${state.id}:${state.horasCompletadas}`;
            if (drawingKey !== key) {
                drawingKey = key;
                drawChart(state.lecturas || []);
                drawTable(state.lecturas || []);
            }
        }
        updateButtons();
    }
    async function readResponse(response) {
        if (!response.headers.get('content-type')?.includes('application/json')) {
            throw new Error(response.redirected || response.status === 401 || response.status === 403 ? 'Tu sesión no está disponible o no tiene permiso. Actualiza la página e inicia sesión nuevamente.' : 'El servidor no devolvió una respuesta válida. Inténtalo de nuevo.');
        }
        const body = await response.json();
        if (!response.ok) throw new Error(body.error || 'No se pudo completar la operación.');
        return body;
    }
    async function refresh() {
        const currentEpoch = epoch;
        try {
            if (!busy && !document.hidden) {
                const next = await readResponse(await fetch(summary.dataset.estadoUrl, { cache: 'no-store', headers: { Accept: 'application/json' } }));
                if (currentEpoch === epoch) render(next);
            }
        } catch (error) {
            if (currentEpoch === epoch) text('conexion', `No se pudo actualizar la simulación. ${error.message} Reintentando…`);
        } finally { refreshTimer = window.setTimeout(refresh, 2000); }
    }
    async function mutate(action, data = new URLSearchParams()) {
        if (busy || !state) return;
        busy = true;
        epoch++;
        updateButtons();
        errorBox.hidden = true;
        result.textContent = action === 'Guardar' ? 'Guardando las lecturas en la base de datos…' : 'Actualizando la simulación…';
        data.set('__RequestVerificationToken', form.elements.namedItem('__RequestVerificationToken').value);
        data.set('id', state.id);
        try {
            const base = page.dataset.baseUrl.replace(/\/Index\/?$/i, '').replace(/\/$/, '');
            const next = await readResponse(await fetch(`${base}/${action}`, { method: 'POST', body: data, headers: { Accept: 'application/json' } }));
            if (action === 'Configurar') { formDirty = false; formId = null; }
            render(next);
            const messages = { Configurar: 'Escenario aplicado. Puedes iniciar una nueva demostración.', Iniciar: 'Simulación en ejecución. Puedes volver al panel principal para seguir los resultados.', Pausar: 'Simulación pausada.', Avanzar: 'Se generó una lectura horaria.', Completar: 'Día simulado completo: 24 lecturas listas para guardar.', Guardar: 'Lecturas guardadas. Ya están disponibles en el proyecto.' };
            result.textContent = messages[action] || 'Operación completada.';
        } catch (error) {
            errorBox.textContent = error.message;
            errorBox.hidden = false;
            result.textContent = 'La operación no se completó.';
        } finally {
            busy = false;
            updateButtons();
        }
    }
    const svgNamespace = 'http://www.w3.org/2000/svg';
    function svgElement(tag, attributes, label) {
        const element = document.createElementNS(svgNamespace, tag);
        Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, value));
        if (label !== undefined) element.textContent = label;
        return element;
    }
    function drawChart(readings) {
        const chart = document.getElementById('sim-grafico');
        chart.replaceChildren();
        const left = 58, top = 20, width = 720, height = 210;
        const maximum = Math.max(0.5, ...readings.flatMap(row => [Number(row.generacionKwh), Number(row.consumoKwh)]));
        const upper = Math.ceil(maximum * 10) / 10;
        for (let i = 0; i <= 4; i++) {
            const y = top + height * i / 4;
            chart.append(svgElement('line', { x1: left, x2: left + width, y1: y, y2: y, class: 'sim-cuadricula' }));
            chart.append(svgElement('text', { x: left - 10, y: y + 4, 'text-anchor': 'end', class: 'sim-eje' }, number(upper * (1 - i / 4))));
        }
        for (let hour = 0; hour <= 24; hour += 3) chart.append(svgElement('text', { x: left + width * hour / 24, y: top + height + 26, 'text-anchor': 'middle', class: 'sim-eje' }, `${String(hour).padStart(2, '0')}:00`));
        ['generacionKwh', 'consumoKwh'].forEach((key, index) => {
            const coords = readings.map((row, i) => [left + width * (i + 0.5) / 24, top + height * (1 - Number(row[key]) / upper)]);
            const className = index === 0 ? 'sim-linea-solar' : 'sim-linea-consumo';
            if (coords.length > 1) chart.append(svgElement('polyline', { points: coords.map(pair => pair.join(',')).join(' '), class: className }));
            coords.forEach(([x, y], i) => {
                const circle = svgElement('circle', { cx: x, cy: y, r: 3.5, class: index === 0 ? 'sim-punto-solar' : 'sim-punto-consumo' });
                circle.append(svgElement('title', {}, `${hourLabel(readings[i].hora)} · ${index === 0 ? 'Generación' : 'Consumo'}: ${number(readings[i][key])} kWh`));
                chart.append(circle);
            });
        });
        document.querySelector('[data-sim-grafica-vacia]').hidden = readings.length > 0;
    }
    function drawTable(readings) {
        const table = document.getElementById('sim-lecturas');
        table.replaceChildren();
        if (!readings.length) {
            const row = table.insertRow();
            const cell = row.insertCell();
            cell.colSpan = 11;
            cell.textContent = 'Aún no hay lecturas. Inicia la demostración.';
            return;
        }
        readings.forEach(reading => {
            const row = table.insertRow();
            const values = [hourLabel(reading.hora), number(reading.irradianciaWm2, 0), number(reading.temperaturaAmbienteC, 1), number(reading.temperaturaPanelC, 1), number(reading.potenciaSolarW, 0), number(reading.potenciaConsumoW, 0), ...['generacionKwh', 'consumoKwh', 'autoconsumoKwh', 'importacionKwh', 'excedenteKwh'].map(key => number(reading[key]))];
            values.forEach(value => { row.insertCell().textContent = value; });
        });
    }
    if (form) {
        form.addEventListener('input', () => { formDirty = true; });
        form.addEventListener('submit', event => {
            event.preventDefault();
            if (!form.reportValidity() || busy) return;
            if (state?.horasCompletadas > 0 && !window.confirm('¿Aplicar este escenario y reiniciar la demostración compartida? Las lecturas actuales sin guardar se descartarán. Los registros ya guardados se conservan.')) return;
            mutate('Configurar', new URLSearchParams(new FormData(form)));
        });
        buttons.forEach(button => button.addEventListener('click', () => mutate(button.dataset.accion)));
        document.querySelector('[data-sim-csv]').addEventListener('click', event => { if (event.currentTarget.getAttribute('aria-disabled') === 'true') event.preventDefault(); });
    }
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) { window.clearTimeout(refreshTimer); refresh(); }
    });
    updateButtons();
    refresh();
})();
