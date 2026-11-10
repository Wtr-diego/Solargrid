// SolarGrid: vista de todas las ubicaciones de los paneles de la base de datos.
// Solo envía cambios al servidor cuando se pulsa "Guardar ubicación".
(() => {
  'use strict';

  const mapaElemento = document.getElementById('mapaPaneles');
  if (!mapaElemento) return;

  const paneles = JSON.parse(document.getElementById('datosPaneles').textContent);
  const filtro = document.getElementById('filtroComunidad');
  const seleccion = document.getElementById('panelSeleccionado');
  const latitudCampo = document.getElementById('latitudPanel');
  const longitudCampo = document.getElementById('longitudPanel');
  const textoLatitud = document.getElementById('textoLatitud');
  const textoLongitud = document.getElementById('textoLongitud');
  const estado = document.getElementById('estadoSeleccion');
  const botonGuardar = document.getElementById('guardarUbicacion');
  const botonQuitar = document.getElementById('quitarUbicacion');
  const idPanelQuitar = document.getElementById('idPanelQuitar');
  const lista = document.getElementById('listaPaneles');
  const cantidad = document.getElementById('cantidadFiltrados');

  const ubicados = p => Number.isFinite(p.Latitud) && Number.isFinite(p.Longitud);
  const visibles = () => paneles.filter(p => !filtro.value || String(p.IdComunidad) === filtro.value);
  const seleccionado = () => paneles.find(p => String(p.IdEquipo) === seleccion.value);

  let mapa = null;
  let marcadores = null;
  let marcaTemporal = null;

  function mensaje(texto) { estado.textContent = texto; }
  function limpiarPreview() {
    latitudCampo.value = '';
    longitudCampo.value = '';
    textoLatitud.textContent = '—';
    textoLongitud.textContent = '—';
    botonGuardar.disabled = true;
    if (marcaTemporal && mapa) mapa.removeLayer(marcaTemporal);
    marcaTemporal = null;
  }

  function elegirPanel(id) {
    seleccion.value = String(id);
    seleccion.dispatchEvent(new Event('change'));
    if (mapa) mapaElemento.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  function cargarLista() {
    const vista = visibles();
    lista.replaceChildren();
    cantidad.textContent = `${vista.length} panel${vista.length === 1 ? '' : 'es'}`;
    if (!vista.length) {
      const mensajeVacio = document.createElement('p');
      mensajeVacio.className = 'secundario';
      mensajeVacio.textContent = 'No hay paneles para esta comunidad. Puedes registrarlos desde Equipos.';
      lista.appendChild(mensajeVacio);
      return;
    }

    vista.forEach(p => {
      const fila = document.createElement('div');
      fila.className = 'fila-panel-mapa';
      const informacion = document.createElement('div');
      informacion.className = 'info-panel-mapa';
      const nombre = document.createElement('strong');
      nombre.textContent = p.NombreVisible;
      const detalle = document.createElement('span');
      detalle.textContent = `${p.Codigo} · ${p.Comunidad} · ${p.Sistema}`;
      const estadoUbicacion = document.createElement('span');
      estadoUbicacion.className = ubicados(p) ? 'chip chip-ubicado' : 'chip chip-pendiente';
      estadoUbicacion.textContent = ubicados(p) ? 'En el mapa' : 'Sin ubicación';
      informacion.append(nombre, detalle);
      const boton = document.createElement('button');
      boton.type = 'button';
      boton.className = 'boton boton-contorno';
      boton.textContent = ubicados(p) ? 'Ver / mover' : 'Ubicar';
      boton.addEventListener('click', () => elegirPanel(p.IdEquipo));
      fila.append(informacion, estadoUbicacion, boton);
      lista.appendChild(fila);
    });
  }

  function actualizarSeleccion() {
    limpiarPreview();
    const p = seleccionado();
    idPanelQuitar.value = p ? String(p.IdEquipo) : '';
    botonQuitar.disabled = !p || !ubicados(p);
    if (!p) {
      mensaje('Selecciona un panel para comenzar.');
      return;
    }
    if (ubicados(p)) {
      mensaje(`El panel ${p.Codigo} ya tiene ubicación. Haz clic en otro punto para cambiarla.`);
      if (mapa) mapa.setView([p.Latitud, p.Longitud], Math.max(mapa.getZoom(), 15));
    } else {
      mensaje(`Panel ${p.Codigo} seleccionado. Haz clic sobre el mapa para asignarle un lugar.`);
    }
  }

  function ponerPreview(lat, lon) {
    const p = seleccionado();
    if (!p || !mapa) return;
    limpiarPreview();
    latitudCampo.value = lat.toFixed(6);
    longitudCampo.value = lon.toFixed(6);
    textoLatitud.textContent = latitudCampo.value;
    textoLongitud.textContent = longitudCampo.value;
    botonGuardar.disabled = false;
    marcaTemporal = L.circleMarker([lat, lon], {
      color: '#b46b00', fillColor: '#ffd37c', fillOpacity: 0.92,
      radius: 12, weight: 3
    }).addTo(mapa);
    mensaje(`Nueva ubicación seleccionada para ${p.Codigo}. Pulsa «Guardar ubicación» para conservarla.`);
  }

  function cargarMarcadores() {
    if (!mapa) return;
    marcadores.clearLayers();
    const limites = [];
    visibles().filter(ubicados).forEach(p => {
      const icono = L.divIcon({ className: 'pin-solar', html: '<span aria-hidden="true">☀</span>', iconSize: [35, 35], iconAnchor: [17, 17] });
      const pin = L.marker([p.Latitud, p.Longitud], { icon: icono }).addTo(marcadores);
      const contenido = document.createElement('div');
      const titulo = document.createElement('strong');
      titulo.textContent = p.Codigo;
      const descripcion = document.createElement('p');
      descripcion.textContent = `${p.Comunidad} · ${p.Sistema}\nEstado: ${p.Estado}`;
      descripcion.style.whiteSpace = 'pre-line';
      const boton = document.createElement('button');
      boton.className = 'boton boton-primario';
      boton.type = 'button';
      boton.textContent = 'Cambiar ubicación';
      boton.addEventListener('click', () => elegirPanel(p.IdEquipo));
      contenido.append(titulo, descripcion, boton);
      pin.bindPopup(contenido);
      limites.push([p.Latitud, p.Longitud]);
    });
    if (limites.length > 1) mapa.fitBounds(limites, { padding: [45, 45], maxZoom: 15 });
    if (limites.length === 1) mapa.setView(limites[0], 14);
    if (!limites.length) mapa.setView([14.0445, -88.9334], 11);
  }

  seleccion.addEventListener('change', actualizarSeleccion);
  filtro.addEventListener('change', () => {
    [...seleccion.options].forEach(op => {
      if (!op.value) return;
      op.hidden = !!filtro.value && op.dataset.comunidad !== filtro.value;
    });
    if (seleccion.value && !visibles().some(p => String(p.IdEquipo) === seleccion.value)) {
      seleccion.value = '';
      actualizarSeleccion();
    }
    cargarLista();
    cargarMarcadores();
  });

  document.getElementById('formQuitarUbicacion').addEventListener('submit', event => {
    const p = seleccionado();
    if (!p || !ubicados(p) || !confirm(`¿Quitar la ubicación de ${p.Codigo}? El equipo seguirá guardado.`)) {
      event.preventDefault();
    }
  });

  document.getElementById('formUbicarPanel').addEventListener('submit', event => {
    if (!seleccionado() || !latitudCampo.value || !longitudCampo.value) {
      event.preventDefault();
      mensaje('Debes escoger un panel y un punto del mapa antes de guardar.');
    }
  });

  cargarLista();
  actualizarSeleccion();
  if (typeof L === 'undefined') {
    mapaElemento.replaceChildren();
    const aviso = document.createElement('p');
    aviso.className = 'error-mapa mapa-aviso';
    aviso.textContent = 'No se pudo cargar el mapa. Comprueba que tienes conexión a Internet y recarga la página.';
    mapaElemento.appendChild(aviso);
    return;
  }

  mapaElemento.replaceChildren();
  mapa = L.map(mapaElemento, { scrollWheelZoom: true }).setView([14.0445, -88.9334], 11);
  L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    maxZoom: 19
  }).addTo(mapa);
  marcadores = L.layerGroup().addTo(mapa);
  cargarMarcadores();
  mapa.on('click', e => ponerPreview(e.latlng.lat, e.latlng.lng));
})();
