// Definición de variables globales para compatibilidad total
window.API_COMPRAS_URL = window.API_COMPRAS_URL || 'https://pounce-chive-erratic.ngrok-free.dev/api/Compras';
window.API_PROVEEDORES_URL = window.API_PROVEEDORES_URL || 'https://pounce-chive-erratic.ngrok-free.dev/api/Proveedores';

var API_COMPRAS_URL = window.API_COMPRAS_URL;
var API_PROVEEDORES_URL = window.API_PROVEEDORES_URL;

// 1. Cargar las órdenes de compra desde la API
async function cargarCompras() {
    const tbody = document.getElementById('tbodyCompras') || document.querySelector('tbody');
    if (!tbody) return;

    try {
        let respuesta = await fetch(`${API_COMPRAS_URL}/listar`, {
            headers: {
                'Accept': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            }
        });

        // Fallback si el controlador responde directo en la raíz
        if (respuesta.status === 404) {
            respuesta = await fetch(API_COMPRAS_URL, {
                headers: {
                    'Accept': 'application/json',
                    'ngrok-skip-browser-warning': 'true'
                }
            });
        }

        if (!respuesta.ok) throw new Error(`HTTP Error: ${respuesta.status}`);

        const compras = await respuesta.json();
        renderizarTablaCompras(compras);
    } catch (error) {
        console.error('Error al conectar con la API de compras:', error);
        tbody.innerHTML = `<tr><td colspan="6" class="table-msg-error" style="text-align:center; padding:15px; color:#ef4444;">Error al cargar compras de la base de datos.</td></tr>`;
    }
}

// 2. Renderizar filas en la tabla
function renderizarTablaCompras(compras) {
    const tbody = document.getElementById('tbodyCompras') || document.querySelector('tbody');
    if (!tbody) return;

    tbody.innerHTML = '';

    if (!Array.isArray(compras) || compras.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" class="table-msg-empty" style="text-align:center; padding:15px;">No hay órdenes de compra registradas.</td></tr>`;
        return;
    }

    compras.forEach(c => {
        const idCompra = c.idCompra || c.IdCompra;
        const oc = c.numeroOC || c.NumeroOC || `OC-${idCompra}`;
        const fecha = c.fechaCompra || c.FechaCompra || '-';
        const proveedor = c.proveedor || c.Proveedor || 'Proveedor General';
        const totalNum = parseFloat(c.totalCosto || c.TotalCosto || 0);
        const total = totalNum.toLocaleString('en-US', { style: 'currency', currency: 'USD' });
        
        const estado = c.estadoRecepcion || c.EstadoRecepcion || 'Recibido';
        const estadoClass = estado.toLowerCase().includes('recibido') ? 'recibido' : 'pendiente';

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><strong>${oc}</strong></td>
            <td>${fecha}</td>
            <td>${proveedor}</td>
            <td>${total}</td>
            <td><span class="badge-status ${estadoClass}">${estado}</span></td>
            <td>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="window.generarPDFCompra(${idCompra})" title="Ver / Descargar PDF">
                    📄 PDF
                </button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

// 3. Cargar proveedores reales dinámicamente
async function cargarSelectProveedores() {
    const select = document.getElementById('selectProveedorCompra');
    if (!select) return;

    try {
        let res = await fetch(`${API_PROVEEDORES_URL}/listar`, {
            headers: {
                'Accept': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            }
        });

        if (res.status === 404) {
            res = await fetch(API_PROVEEDORES_URL, {
                headers: {
                    'Accept': 'application/json',
                    'ngrok-skip-browser-warning': 'true'
                }
            });
        }

        if (!res.ok) throw new Error('Error al obtener la lista de proveedores.');

        const proveedores = await res.json();
        select.innerHTML = '<option value="">-- Seleccione un Proveedor --</option>';

        proveedores.forEach(p => {
            const id = p.idProveedor || p.IdProveedor;
            const razon = p.razonSocial || p.RazonSocial || p.nombre || p.Nombre;
            const opt = document.createElement('option');
            opt.value = id;
            opt.textContent = razon;
            select.appendChild(opt);
        });
    } catch (err) {
        console.error('Error cargando combo proveedores:', err);
        select.innerHTML = '<option value="">Error al cargar proveedores</option>';
    }
}

// 4. Generar y abrir el PDF usando clases CSS limpias
window.generarPDFCompra = async function(idCompra) {
    try {
        const res = await fetch(`${API_COMPRAS_URL}/detalle/${idCompra}`, {
            headers: {
                'Accept': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            }
        });

        if (!res.ok) throw new Error('No se pudo obtener el detalle de la compra.');

        const data = await res.json();

        const nroOC = data.numeroOC || data.NumeroOC || `OC-${idCompra}`;
        const nomProv = data.proveedor || data.Proveedor || 'General';
        const fCompra = data.fechaCompra || data.FechaCompra || '-';
        const rucProv = data.rucProveedor || data.RucProveedor || '-';
        const estRec = data.estadoRecepcion || data.EstadoRecepcion || 'Recibido';
        const dirProv = data.direccion || data.Direccion || '-';
        const telProv = data.telefono || data.Telefono || '-';
        const costoTotal = parseFloat(data.totalCosto || data.TotalCosto || 0).toFixed(2);

        const element = document.createElement('div');
        element.className = 'pdf-container';

        element.innerHTML = `
            <div class="pdf-header">
                <div class="pdf-empresa">
                    <h2>MULTI VENTAS S.A.C.</h2>
                    <p>RUC: 20123456789 | Huancayo, Perú</p>
                    <p>Email: compras@multiventas.com</p>
                </div>
                <div class="pdf-box-oc">
                    <h3>ORDEN DE COMPRA</h3>
                    <p>${nroOC}</p>
                </div>
            </div>

            <div class="pdf-card-datos">
                <h4>Datos del Proveedor</h4>
                <table class="pdf-table-datos">
                    <tr>
                        <td class="pdf-col-20"><strong>Razón Social:</strong></td>
                        <td>${nomProv}</td>
                        <td class="pdf-col-15"><strong>Fecha:</strong></td>
                        <td>${fCompra}</td>
                    </tr>
                    <tr>
                        <td><strong>RUC:</strong></td>
                        <td>${rucProv}</td>
                        <td><strong>Estado:</strong></td>
                        <td>${estRec}</td>
                    </tr>
                    <tr>
                        <td><strong>Dirección:</strong></td>
                        <td colspan="3">${dirProv} (Tel: ${telProv})</td>
                    </tr>
                </table>
            </div>

            <table class="pdf-table-items">
                <thead>
                    <tr>
                        <th class="pdf-th-item">Ítem / Descripción</th>
                        <th class="pdf-th-qty">Cantidad</th>
                        <th class="pdf-th-total">Total</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Abastecimiento General de Mercadería / Inventario</td>
                        <td class="pdf-td-center">1 Lote</td>
                        <td class="pdf-td-right">$${costoTotal}</td>
                    </tr>
                </tbody>
            </table>

            <div class="pdf-total-container">
                <table class="pdf-table-total">
                    <tr class="pdf-row-total">
                        <td>TOTAL COSTO:</td>
                        <td class="pdf-total-val">$${costoTotal}</td>
                    </tr>
                </table>
            </div>

            <div class="pdf-firmas-container">
                <div class="pdf-firma-box">Firma Encargado de Compras</div>
                <div class="pdf-firma-box">Firma / Sello Recepción</div>
            </div>
        `;

        const opciones = {
            margin: 10,
            filename: `${nroOC}.pdf`,
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: { scale: 2 },
            jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
        };

        html2pdf().set(opciones).from(element).toPdf().get('pdf').then(function(pdfObj) {
            window.open(pdfObj.output('bloburl'), '_blank');
        });

    } catch (err) {
        console.error(err);
        alert(`Error al generar el PDF: ${err.message}`);
    }
};

// 5. Inicializar modal y eventos
document.addEventListener('DOMContentLoaded', () => {
    cargarCompras();
    cargarSelectProveedores();

    const modal = document.getElementById('modalCompra');
    const btnCerrar = document.getElementById('btnCerrarModalCompra');
    const btnCancelar = document.getElementById('btnCancelarCompra');
    const form = document.getElementById('formNuevaCompra');
    const btnRegistrar = document.getElementById('btnRegistrarOrden') || Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Registrar Orden'));

    const abrirModal = (e) => {
        if (e) e.preventDefault();
        cargarSelectProveedores();
        if (modal) modal.classList.add('active');
    };

    const cerrarModal = () => {
        if (modal) modal.classList.remove('active');
    };

    if (btnRegistrar) {
        btnRegistrar.onclick = null;
        btnRegistrar.addEventListener('click', abrirModal);
    }

    if (btnCerrar) btnCerrar.addEventListener('click', cerrarModal);
    if (btnCancelar) btnCancelar.addEventListener('click', cerrarModal);

    if (modal) {
        window.addEventListener('click', (e) => {
            if (e.target === modal) cerrarModal();
        });
    }

    // Guardar nueva orden de compra
    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();

            const selectProv = document.getElementById('selectProveedorCompra');
            const idProv = selectProv ? selectProv.value : '';
            const total = document.getElementById('inputTotalCompra')?.value;
            const nroOC = document.getElementById('inputNroOC')?.value;
            const estado = document.getElementById('selectEstadoCompra')?.value || 'Recibido';

            if (!idProv) {
                alert('Por favor selecciona un proveedor de la lista.');
                return;
            }

            if (!total || parseFloat(total) <= 0) {
                alert('Ingresa un monto total válido.');
                return;
            }

            // Obtener el ID del usuario en sesión si está disponible
            let userId = 1;
            try {
                const sesion = JSON.parse(localStorage.getItem('usuarioSesion') || '{}');
                if (sesion.idUsuario || sesion.IdUsuario) {
                    userId = sesion.idUsuario || sesion.IdUsuario;
                }
            } catch (ex) {
                userId = 1;
            }

            const nuevaCompra = {
                idProveedor: parseInt(idProv),
                IdProveedor: parseInt(idProv),
                idUsuario: parseInt(userId),
                IdUsuario: parseInt(userId),
                numeroOC: nroOC ? nroOC.trim() : '',
                NumeroOC: nroOC ? nroOC.trim() : '',
                totalCosto: parseFloat(total),
                TotalCosto: parseFloat(total),
                estadoRecepcion: estado,
                EstadoRecepcion: estado
            };

            try {
                let res = await fetch(`${API_COMPRAS_URL}/guardar`, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Accept': 'application/json',
                        'ngrok-skip-browser-warning': 'true'
                    },
                    body: JSON.stringify(nuevaCompra)
                });

                if (res.status === 404) {
                    res = await fetch(API_COMPRAS_URL, {
                        method: 'POST',
                        headers: { 
                            'Content-Type': 'application/json',
                            'Accept': 'application/json',
                            'ngrok-skip-browser-warning': 'true'
                        },
                        body: JSON.stringify(nuevaCompra)
                    });
                }

                if (!res.ok) throw new Error(await res.text());

                alert('¡Orden de compra registrada exitosamente en SQL Server!');
                form.reset();
                cerrarModal();
                cargarCompras();
            } catch (err) {
                console.error(err);
                alert(`Error al registrar compra: ${err.message}`);
            }
        });
    }
});