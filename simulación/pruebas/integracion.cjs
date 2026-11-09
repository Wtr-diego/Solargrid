/* Ejecutar únicamente contra una instancia conectada a una base de pruebas aislada.
 * PowerShell:
 * $env:SOLARGRID_TEST_DB_CONFIRM='SolarGrid_Pruebas_Simulacion_20261108'
 * node 'simulación/pruebas/integracion.cjs'
 * BASE_URL permite cambiar http://127.0.0.1:5187; la confirmación es obligatoria.
 */
'use strict';
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const liveOnly = process.argv.includes('--home-live-only');
const base = (process.env.BASE_URL || 'http://127.0.0.1:5187').replace(/\/$/, '');
const dbConfirmada = process.env.SOLARGRID_TEST_DB_CONFIRM || '';
if (!/^SolarGrid_Pruebas_[A-Za-z0-9_]+$/.test(dbConfirmada)) {
    console.error('No se ejecutaron pruebas. Confirma primero la base aislada mediante SOLARGRID_TEST_DB_CONFIRM=SolarGrid_Pruebas_...; el guion crea datos de demostración.');
    process.exit(2);
}
const out = path.resolve(__dirname, '../evidencias');
fs.mkdirSync(out, { recursive: true });
const results = [], jsErrors = [], consoleErrors = [];
const metadata = { fechaEjecucionUtc: new Date().toISOString(), baseUrl: base, baseDePruebasConfirmadaPorOperador: dbConfirmada };
function writeResults() { fs.writeFileSync(path.join(out, liveOnly ? 'resultados-inicio-en-vivo.json' : 'resultados-integracion.json'), JSON.stringify({ ...metadata, resultados: results, erroresJavaScript: jsErrors, erroresConsola: consoleErrors }, null, 2)); }
async function test(id, nombre, entrada, esperado, action) {
    try {
        const obtenido = await action();
        results.push({ id, nombre, entrada, esperado, obtenido, estado: 'Aprobada' });
        console.log(`${id} APROBADA | ${nombre}`);
        writeResults();
        return obtenido;
    } catch (error) {
        results.push({ id, nombre, entrada, esperado, obtenido: error.message, estado: 'Fallida' });
        console.error(`${id} FALLIDA | ${nombre}: ${error.message}`);
        writeResults();
        throw error;
    }
}
const near = (actual, expected) => assert.ok(Math.abs(actual - expected) < 0.000001, `${actual} != ${expected}`);
const format = value => new Intl.NumberFormat('es', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
const numericText = value => Number(value.replace(/\s*kWh\s*$/, '').replace(',', '.').trim());

(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    const context = await browser.newContext({ viewport: { width: 1440, height: 1080 }, locale: 'es-SV', colorScheme: 'light' });
    const page = await context.newPage();
    page.setDefaultTimeout(15000);
    page.on('pageerror', error => jsErrors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') consoleErrors.push({ pagina: page.url(), mensaje: message.text() }); });
    page.on('dialog', dialog => dialog.accept());
    const go = route => page.goto(base + route, { waitUntil: 'domcontentloaded' });
    async function login(role) {
        await context.clearCookies();
        await go('/Cuenta/Ingresar');
        await page.locator('#Correo').fill(`${role}@solargrid.local`);
        await page.locator('#Contrasena').fill('DemoSolar2026!');
        await Promise.all([page.waitForURL(url => !url.pathname.includes('/Cuenta/Ingresar')), page.getByRole('button', { name: 'Entrar al sistema' }).click()]);
    }
    const state = async () => {
        const response = await context.request.get(base + '/Simulacion/Estado');
        assert.equal(response.status(), 200);
        return response.json();
    };
    let token;
    async function post(action, fields = {}, expectedStatus = 200) {
        const response = await context.request.post(base + '/Simulacion/' + action, { form: { __RequestVerificationToken: token, ...fields } });
        assert.equal(response.status(), expectedStatus, await response.text());
        return response.json();
    }
    async function uiAction(action) {
        const responsePromise = page.waitForResponse(response => response.url().endsWith('/Simulacion/' + action) && response.request().method() === 'POST');
        await page.locator(`[data-accion="${action}"]`).click();
        const response = await responsePromise;
        assert.equal(response.status(), 200, await response.text());
        await page.locator('#sim-error').waitFor({ state: 'hidden' });
        return response.json();
    }
    async function waitHours(minimum) {
        const until = Date.now() + 12000;
        while (Date.now() < until) {
            const current = await state();
            if (current.horasCompletadas >= minimum) return current;
            await new Promise(resolve => setTimeout(resolve, 300));
        }
        throw new Error(`El reloj no alcanzó ${minimum} horas.`);
    }
    try {
        await test('I01', 'Acceso anónimo protegido', 'GET /Simulacion sin sesión', 'Redirección al inicio de sesión', async () => {
            await go('/Simulacion');
            assert.ok(page.url().includes('/Cuenta/Ingresar'));
            return page.url();
        });
        await test('I02', 'Usuario de comunidad sin permisos', 'Cuenta vecino', 'Acceso denegado al módulo y a su estado', async () => {
            await login('vecino');
            await go('/Simulacion');
            assert.ok(page.url().includes('/Cuenta/SinPermiso'));
            const response = await context.request.get(base + '/Simulacion/Estado', { maxRedirects: 0 });
            assert.equal(response.status(), 302);
            assert.ok(response.headers().location.includes('/Cuenta/SinPermiso'));
            return { pagina: page.url(), estadoHttp: response.status() };
        });
        await test('I03', 'Técnico autorizado', 'Cuenta tecnico', 'Módulo y JSON accesibles', async () => {
            await login('tecnico');
            await go('/Simulacion');
            assert.equal(await page.locator('h1').innerText(), 'Simulación solar');
            assert.equal((await state()).totalHoras, 24);
            return 'Módulo visible; API HTTP 200; total de 24 horas.';
        });
        await test('I04', 'Administrador, título y navegación', 'Cuenta admin', 'Módulo visible, título SolarGrid y enlace en el menú', async () => {
            await login('admin');
            await go('/Simulacion');
            await page.locator('[data-configurar]:enabled').waitFor();
            assert.equal(await page.title(), 'SolarGrid · Simulación solar');
            assert.equal(await page.locator('.sidebar a[href="/Simulacion"]').count(), 1);
            assert.equal(await page.locator('link[rel="icon"]').count(), 1);
            token = await page.locator('#sim-config-form input[name="__RequestVerificationToken"]').inputValue();
            return { titulo: await page.title(), favicon: await page.locator('link[rel="icon"]').getAttribute('href') };
        });
        if (liveOnly) {
            await test('I27', 'Actualización automática del panel principal', 'Nuevo escenario en ejecución; navegar a Inicio sin recargar', 'Progreso y consumo crecen en la página principal mientras el servidor genera datos', async () => {
                const previous = await state();
                if (previous.estado === 'En ejecución') await post('Pausar', { id: previous.id });
                const fresh = await post('Configurar', { id: previous.id, Ubicacion: 'chalatenango', Clima: 'Soleado', PotenciaPanelW: '450', CantidadPaneles: '1', FactorConsumo: '1' });
                await post('Iniciar', { id: fresh.id });
                await go('/');
                await page.waitForFunction(() => document.getElementById('sim-progreso')?.value > 0);
                const first = await page.locator('#sim-progreso').evaluate(element => element.value);
                const firstConsumption = await page.locator('[data-sim="consumoKwh"]').innerText();
                await page.waitForFunction(value => document.getElementById('sim-progreso')?.value >= value + 2, first);
                const second = await page.locator('#sim-progreso').evaluate(element => element.value);
                const secondConsumption = await page.locator('[data-sim="consumoKwh"]').innerText();
                assert.ok(second > first);
                assert.ok(numericText(secondConsumption) > numericText(firstConsumption));
                assert.equal(await page.locator('[data-sim="estado"]').innerText(), 'En ejecución');
                assert.equal(jsErrors.length, 0, JSON.stringify(jsErrors));
                assert.equal(consoleErrors.length, 0, JSON.stringify(consoleErrors));
                await page.screenshot({ path: path.join(out, 'panel-principal-en-ejecucion.png'), fullPage: true });
                await post('Pausar', { id: fresh.id });
                return { horasIniciales: first, horasFinales: second, consumoInicialKwh: firstConsumption, consumoFinalKwh: secondConsumption, recargas: 0, evidencia: 'panel-principal-en-ejecucion.png' };
            });
            metadata.resumen = 'Comprobación adicional de actualización del inicio aprobada.';
            writeResults();
            console.log(metadata.resumen);
            return;
        }
        await test('I05', 'Antifalsificación', 'POST Iniciar sin token', 'HTTP 400 y ninguna lectura generada', async () => {
            const before = await state();
            const response = await context.request.post(base + '/Simulacion/Iniciar', { form: { id: before.id } });
            assert.equal(response.status(), 400);
            assert.equal((await state()).horasCompletadas, before.horasCompletadas);
            return 'HTTP 400; estado conservado.';
        });
        const config = { Ubicacion: 'chalatenango', Clima: 'Soleado', PotenciaPanelW: '450', CantidadPaneles: '1', FactorConsumo: '1' };
        const invalidCases = [
            ['I06', 'ubicación desconocida', { Ubicacion: 'no-existe' }],
            ['I07', 'clima desconocido', { Clima: 'TormentaInexistente' }],
            ['I08', 'potencia fuera de rango', { PotenciaPanelW: '0' }],
            ['I09', 'potencia sin múltiplo de 10', { PotenciaPanelW: '455' }],
            ['I10', 'cantidad fuera de rango', { CantidadPaneles: '25' }],
            ['I11', 'factor de consumo fuera de rango', { FactorConsumo: '0' }]
        ];
        for (const [id, description, invalid] of invalidCases) {
            await test(id, 'Validación: ' + description, invalid, 'HTTP 400, mensaje y sesión intacta', async () => {
                const before = await state();
                const response = await post('Configurar', { ...config, ...invalid, id: before.id }, 400);
                assert.ok(response.error);
                assert.equal((await state()).id, before.id);
                return response;
            });
        }
        let oldId;
        await test('I12', 'Aplicar escenario desde la interfaz', config, 'Chalatenango, 450 W, soleado, 0 lecturas y sesión nueva', async () => {
            oldId = (await state()).id;
            await page.locator('#sim-ubicacion').selectOption(config.Ubicacion);
            await page.locator('#sim-clima').selectOption(config.Clima);
            await page.locator('#sim-potencia').fill(config.PotenciaPanelW);
            await page.locator('#sim-paneles').fill(config.CantidadPaneles);
            await page.locator('#sim-consumo').fill(config.FactorConsumo);
            const responsePromise = page.waitForResponse(response => response.url().endsWith('/Simulacion/Configurar') && response.request().method() === 'POST');
            await page.locator('[data-configurar]').click();
            const response = await responsePromise;
            assert.equal(response.status(), 200, await response.text());
            const current = await response.json();
            assert.notEqual(current.id, oldId);
            assert.equal(current.configuracion.ubicacion, 'chalatenango');
            assert.equal(current.potenciaInstaladaW, 450);
            assert.equal(current.horasCompletadas, 0);
            return { id: current.id, lugar: current.lugar, configuracion: current.configuracion };
        });
        await test('I13', 'No guardar una simulación incompleta', 'Guardar con 0 horas', 'HTTP 409 y mensaje claro', async () => post('Guardar', { id: (await state()).id }, 409));
        await test('I14', 'Protección contra una pestaña con sesión anterior', 'Iniciar con ID previo al reinicio', 'HTTP 409 sin comenzar el reloj', async () => {
            const response = await post('Iniciar', { id: oldId }, 409);
            assert.equal((await state()).estado, 'Lista');
            return response;
        });
        await test('I15', 'Reloj automático y pausa', 'Iniciar; esperar 3 horas; pausar y esperar 2.4 segundos', 'Avanza automáticamente y se detiene al pausar', async () => {
            await uiAction('Iniciar');
            await waitHours(3);
            await go('/');
            await page.waitForFunction(() => document.getElementById('sim-progreso')?.value >= 3);
            const liveHour = await page.locator('#sim-progreso').evaluate(element => element.value);
            await page.waitForFunction(value => document.getElementById('sim-progreso')?.value > value, liveHour);
            await page.screenshot({ path: path.join(out, 'panel-principal-en-ejecucion.png'), fullPage: true });
            await go('/Simulacion');
            await page.locator('[data-accion="Pausar"]:enabled').waitFor();
            const paused = await uiAction('Pausar');
            assert.equal(paused.estado, 'Pausada');
            await new Promise(resolve => setTimeout(resolve, 2400));
            assert.equal((await state()).horasCompletadas, paused.horasCompletadas);
            return { horasAlPausar: paused.horasCompletadas, horasDespuesDeEsperar: (await state()).horasCompletadas };
        });
        await test('I16', 'Continuar y avance manual', 'Continuar, pausar y Avanzar 1 hora', 'Reanuda el reloj y agrega exactamente una lectura manual', async () => {
            const before = await state();
            await uiAction('Iniciar');
            await waitHours(before.horasCompletadas + 1);
            const paused = await uiAction('Pausar');
            const advanced = await uiAction('Avanzar');
            assert.equal(advanced.horasCompletadas, paused.horasCompletadas + 1);
            assert.equal(advanced.estado, 'Pausada');
            return { horasAntes: before.horasCompletadas, horasReanudadas: paused.horasCompletadas, horasAvanceManual: advanced.horasCompletadas };
        });
        const completed = await test('I17', 'Completar el día y conservar energía', 'Completar el día', '24 intervalos con balances y totales correctos', async () => {
            const current = await uiAction('Completar');
            assert.equal(current.estado, 'Completada');
            assert.equal(current.lecturas.length, 24);
            for (const row of current.lecturas) {
                near(row.generacionKwh, row.autoconsumoKwh + row.excedenteKwh);
                near(row.consumoKwh, row.autoconsumoKwh + row.importacionKwh);
                assert.equal(row.hora.slice(0, 10), current.fecha.slice(0, 10));
                assert.ok(row.generacionKwh >= 0 && row.consumoKwh >= 0);
            }
            for (const field of ['generacionKwh', 'consumoKwh', 'autoconsumoKwh', 'importacionKwh', 'excedenteKwh'])
                near(current[field], current.lecturas.reduce((sum, row) => sum + row[field], 0));
            fs.writeFileSync(path.join(out, 'estado-simulacion.json'), JSON.stringify(current, null, 2));
            return current;
        });
        let saved;
        await test('I18', 'Guardar e impedir duplicados', 'Guardar desde interfaz; repetir Guardar dos veces por API', 'Un único ID de sistema y 24 lecturas', async () => {
            saved = await uiAction('Guardar');
            assert.ok(saved.idSistemaGuardado > 0);
            const repeat1 = await post('Guardar', { id: saved.id });
            const repeat2 = await post('Guardar', { id: saved.id });
            assert.equal(repeat1.idSistemaGuardado, saved.idSistemaGuardado);
            assert.equal(repeat2.idSistemaGuardado, saved.idSistemaGuardado);
            return { idSistemaGuardado: saved.idSistemaGuardado, repeticiones: 2, lecturas: repeat2.lecturas.length };
        });
        await test('I19', 'Reporte con datos persistidos y sin duplicados', 'Reporte del sistema y fecha simulados', '24 filas; totales iguales al simulador', async () => {
            const day = completed.fecha.slice(0, 10);
            await go(`/Reportes?idSistema=${saved.idSistemaGuardado}&desde=${day}&hasta=${day}`);
            assert.equal(await page.locator('tbody tr').count(), 24);
            const cards = page.locator('.indicadores .indicador strong');
            near(numericText(await cards.nth(0).innerText()), saved.generacionKwh);
            near(numericText(await cards.nth(1).innerText()), saved.consumoKwh);
            assert.equal(await cards.nth(2).innerText(), '24');
            assert.ok((await page.locator('tbody').innerText()).includes('[SIM] Chalatenango'));
            await page.screenshot({ path: path.join(out, 'reporte-guardado.png'), fullPage: true });
            return { filas: 24, generacionKwh: saved.generacionKwh, consumoKwh: saved.consumoKwh, evidencia: 'reporte-guardado.png' };
        });
        await test('I20', 'Panel virtual integrado al mapa', 'Datos serializados de /Comunidades', 'Paneles y coordenadas del escenario guardado', async () => {
            const response = await context.request.get(base + '/Comunidades');
            const html = await response.text();
            const match = html.match(/<script type="application\/json" id="datosPaneles">([\s\S]*?)<\/script>/);
            assert.ok(match, 'No se encontró el conjunto de paneles del mapa.');
            const panels = JSON.parse(match[1]).filter(panel => panel.Sistema.includes(saved.id.replaceAll('-', '').slice(0, 8)));
            assert.equal(panels.length, saved.configuracion.cantidadPaneles);
            for (const panel of panels) {
                near(panel.Latitud, saved.lugar.latitud);
                near(panel.Longitud, saved.lugar.longitud);
                assert.ok(panel.Codigo.startsWith('SIM-'));
            }
            fs.writeFileSync(path.join(out, 'paneles-mapa.json'), JSON.stringify(panels, null, 2));
            return { paneles: panels, alcance: 'Se verificó el contenido del mapa sin cargar Leaflet ni mosaicos externos.' };
        });
        await test('I21', 'CSV completo y etiquetado', 'GET /Simulacion/Exportar', '24 registros, 20 columnas, origen SIMULADO y valores del escenario', async () => {
            const response = await context.request.get(base + '/Simulacion/Exportar');
            assert.equal(response.status(), 200);
            const buffer = await response.body();
            fs.writeFileSync(path.join(out, 'SolarGrid-simulacion.csv'), buffer);
            const lines = buffer.toString('utf8').replace(/^\uFEFF/, '').trim().split(/\r?\n/);
            assert.equal(lines.length, 25);
            assert.equal(lines[0].split(',').length, 20);
            const rows = lines.slice(1).map(line => line.split(','));
            assert.ok(rows.every(row => row.length === 20 && row[0] === 'SIMULADO' && row[1] === saved.id && row[2] === 'Chalatenango'));
            near(rows.reduce((sum, row) => sum + Number(row[15]), 0), saved.generacionKwh);
            near(rows.reduce((sum, row) => sum + Number(row[16]), 0), saved.consumoKwh);
            return { registros: rows.length, columnas: 20, evidencia: 'SolarGrid-simulacion.csv' };
        });
        await test('I22', 'Panel principal conectado', 'Inicio después del guardado', 'Resumen con mismos valores y estado de la simulación', async () => {
            await go('/');
            await page.waitForFunction(expected => document.querySelector('[data-sim="generacionKwh"]')?.textContent === expected, format(saved.generacionKwh));
            for (const field of ['generacionKwh', 'consumoKwh', 'autoconsumoKwh', 'importacionKwh', 'excedenteKwh'])
                assert.equal(await page.locator(`[data-sim="${field}"]`).innerText(), format(saved[field]));
            assert.equal(await page.locator('[data-sim="estado"]').innerText(), 'Completada');
            await page.screenshot({ path: path.join(out, 'panel-principal.png'), fullPage: true });
            await page.locator('[data-simulacion]').screenshot({ path: path.join(out, 'resumen-principal.png') });
            return { valoresCoinciden: true, evidencias: ['panel-principal.png', 'resumen-principal.png'] };
        });
        await test('I23', 'Interfaz completa en escritorio', '1440 × 1080, tema claro', '24 filas, gráfica y página sin desplazamiento horizontal', async () => {
            await go('/Simulacion');
            await page.locator('#sim-lecturas tr').nth(23).waitFor();
            assert.equal(await page.locator('#sim-lecturas tr').count(), 24);
            assert.equal(await page.locator('#sim-grafico circle').count(), 48);
            const sizes = await page.evaluate(() => ({ contenido: document.documentElement.scrollWidth, ventana: innerWidth }));
            assert.ok(sizes.contenido <= sizes.ventana + 1, JSON.stringify(sizes));
            await page.screenshot({ path: path.join(out, 'simulacion-escritorio.png'), fullPage: true });
            await page.screenshot({ path: path.join(out, 'simulacion-escritorio-inicio.png') });
            return { ...sizes, evidencias: ['simulacion-escritorio.png', 'simulacion-escritorio-inicio.png'] };
        });
        await test('I24', 'Tema oscuro', 'Cambiar tema desde el selector', 'Tema oscuro activo y métricas conservadas', async () => {
            await page.locator('#selector-tema').click();
            assert.equal(await page.locator('html').getAttribute('data-tema'), 'dark');
            assert.equal(await page.locator('[data-sim="generacionKwh"]').innerText(), format(saved.generacionKwh));
            await page.screenshot({ path: path.join(out, 'simulacion-oscuro.png') });
            await page.locator('#selector-tema').click();
            return { tema: 'dark', evidencia: 'simulacion-oscuro.png' };
        });
        await test('I25', 'Interfaz móvil', '390 × 844', 'Sin desbordamiento horizontal de la página; tablas con scroll propio', async () => {
            await page.setViewportSize({ width: 390, height: 844 });
            await page.waitForTimeout(200);
            const sizes = await page.evaluate(() => ({ contenido: document.documentElement.scrollWidth, ventana: innerWidth, tabla: document.querySelector('.sim-tabla').scrollWidth, contenedorTabla: document.querySelector('.sim-tabla').clientWidth }));
            await page.screenshot({ path: path.join(out, 'simulacion-movil.png'), fullPage: true });
            assert.ok(sizes.contenido <= sizes.ventana + 1, JSON.stringify(sizes));
            assert.ok(sizes.tabla >= sizes.contenedorTabla);
            return { ...sizes, evidencia: 'simulacion-movil.png' };
        });
        await test('I26', 'Sin errores JavaScript ni consola', 'Navegación y operaciones de las pruebas anteriores', 'Cero errores', async () => {
            assert.equal(jsErrors.length, 0, JSON.stringify(jsErrors));
            assert.equal(consoleErrors.length, 0, JSON.stringify(consoleErrors));
            return { erroresJavaScript: 0, erroresConsola: 0 };
        });
        metadata.idSimulacion = saved.id;
        metadata.idSistemaGuardado = saved.idSistemaGuardado;
        metadata.resumen = `${results.length}/${results.length} pruebas aprobadas`;
        writeResults();
        console.log(metadata.resumen);
    } finally {
        await browser.close();
    }
})().catch(error => { console.error(error.stack); writeResults(); process.exitCode = 1; });
