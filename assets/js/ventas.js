// Variables globales para evitar errores de ámbito y asegurar consistencia
window.API_VENTAS_URL = window.API_VENTAS_URL || 'https://pounce-chive-erratic.ngrok-free.dev/api/Ventas';
window.API_CLIENTES_URL = window.API_CLIENTES_URL || 'https://pounce-chive-erratic.ngrok-free.dev/api/Clientes';

var API_VENTAS_URL = window.API_VENTAS_URL;
var API_CLIENTES_URL = window.API_CLIENTES_URL;

// 1. Cargar la lista de ventas desde SQL Server
async function cargarVentas() {
    const tbody = document.querySelector('tbody');
    if (!tbody) return;

    try {
        let respuesta = await fetch(`${API_VENTAS_URL}/listar`, {
            headers: {
                'Accept': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            }
        });

        if (respuesta.status === 404) {
            respuesta = await fetch(API_VENTAS_URL, {
                headers: {
                    'Accept': 'application/json',
                    'ngrok-skip-browser-warning': 'true'
                }
            });
        }

        if (!respuesta.ok) throw new Error(`HTTP Error: ${respuesta.status}`);

        const ventas = await respuesta.json();
        renderizarTablaVentas(ventas);
    } catch (error) {
        console.error('Error al conectar con la API de ventas:', error);
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; color:#ef4444; padding:15px;">Error al cargar ventas desde la base de datos.</td></tr>`;
    }
}

// 2. Renderizar filas y vincular botones
function renderizarTablaVentas(ventas) {
    const tbody = document.querySelector('tbody');
    if (!tbody) return;

    tbody.innerHTML = '';

    if (!Array.isArray(ventas) || ventas.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; padding:15px; color:#94a3b8;">No hay comprobantes de venta registrados.</td></tr>`;
        return;
    }

    ventas.forEach(v => {
        const idVenta = v.idVenta || v.IdVenta;
        const codigo = v.numeroComprobante || v.NumeroComprobante || `VNT-${idVenta}`;
        const fecha = v.fechaVenta || v.FechaVenta || '-';
        const cliente = v.cliente || v.Cliente || 'Consumidor Final';
        const totalNum = parseFloat(v.total !== undefined ? v.total : (v.Total || 0));
        const total = totalNum.toLocaleString('en-US', { style: 'currency', currency: 'USD' });

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><strong>${codigo}</strong></td>
            <td>${fecha}</td>
            <td>${cliente}</td>
            <td>${total}</td>
            <td><span class="badge bg-info text-dark">Efectivo</span></td>
            <td><span class="badge bg-success">Completada</span></td>
            <td>
                <button type="button" class="btn btn-sm btn-outline-secondary me-1" onclick="window.verDetalleVenta(${idVenta})" title="Ver Detalle">👁</button>
                <button type="button" class="btn btn-sm btn-outline-primary" onclick="window.imprimirComprobante(${idVenta})" title="Imprimir">🖨</button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

// 3. Función global: VER DETALLE (Ojo)
window.verDetalleVenta = async function(idVenta) {
    try {
        let res = await fetch(`${API_VENTAS_URL}/detalle/${idVenta}`, {
            headers: {
                'Accept': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            }
        });

        if (res.status === 404) {
            res = await fetch(`${API_VENTAS_URL}/${idVenta}`, {
                headers: {
                    'Accept': 'application/json',
                    'ngrok-skip-browser-warning': 'true'
                }
            });
        }

        if (!res.ok) throw new Error('No se pudo obtener el detalle de la venta.');

        const data = await res.json();
        const nroComp = data.numeroComprobante || data.NumeroComprobante || `VNT-${idVenta}`;
        const fVenta = data.fechaVenta || data.FechaVenta || '-';
        const cliNom = data.cliente || data.Cliente || 'Consumidor Final';
        const cliDoc = data.documento || data.Documento || '-';
        const cliTel = data.telefono || data.Telefono || '-';
        const cliDir = data.direccion || data.Direccion || '-';
        const tot = parseFloat(data.total !== undefined ? data.total : (data.Total || 0)).toFixed(2);

        alert(
            `==============================\n` +
            `       DETALLE DE VENTA       \n` +
            `==============================\n` +
            `N° Comprobante: ${nroComp}\n` +
            `Fecha:          ${fVenta}\n` +
            `Cliente:        ${cliNom}\n` +
            `Doc/DNI:        ${cliDoc}\n` +
            `Teléfono:       ${cliTel}\n` +
            `Dirección:      ${cliDir}\n` +
            `------------------------------\n` +
            `TOTAL:          $${tot}\n` +
            `Estado:         Completada\n` +
            `==============================`
        );
    } catch (err) {
        console.error(err);
        alert(`Error al ver detalle: ${err.message}`);
    }
};

// 4. Función global: IMPRIMIR (Impresora)
window.imprimirComprobante = async function(idVenta) {
    try {
        let res = await fetch(`${API_VENTAS_URL}/detalle/${idVenta}`, {
            headers: {
                'Accept': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            }
        });

        if (res.status === 404) {
            res = await fetch(`${API_VENTAS_URL}/${idVenta}`, {
                headers: {
                    'Accept': 'application/json',
                    'ngrok-skip-browser-warning': 'true'
                }
            });
        }

        if (!res.ok) throw new Error('No se pudo obtener el comprobante.');

        const data = await res.json();
        const nroComp = data.numeroComprobante || data.NumeroComprobante || `VNT-${idVenta}`;
        const fVenta = data.fechaVenta || data.FechaVenta || '-';
        const cliNom = data.cliente || data.Cliente || 'Consumidor Final';
        const cliDoc = data.documento || data.Documento || '-';
        const cliDir = data.direccion || data.Direccion || '-';
        const tot = parseFloat(data.total !== undefined ? data.total : (data.Total || 0)).toFixed(2);

        const ventana = window.open('', '_blank', 'width=450,height=600');
        if (!ventana) {
            alert('Por favor habilita las ventanas emergentes (pop-ups) en tu navegador para imprimir.');
            return;
        }

        ventana.document.write(`
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>Comprobante - ${nroComp}</title>
                <style>
                    body { font-family: 'Courier New', monospace; padding: 20px; font-size: 13px; color: #000; }
                    .center { text-align: center; }
                    .line { border-top: 1px dashed #000; margin: 10px 0; }
                    .row { display: flex; justify-content: space-between; margin: 4px 0; }
                    .total { font-size: 16px; font-weight: bold; }
                </style>
            </head>
            <body>
                <div class="center">
                    <h2 style="margin: 0;">SISTEMA MULTI VENTAS</h2>
                    <p style="margin: 3px 0;">RUC: 20123456789</p>
                    <p style="margin: 3px 0;"><strong>COMPROBANTE ELECTRÓNICO</strong></p>
                    <p style="margin: 3px 0;">${nroComp}</p>
                </div>
                <div class="line"></div>
                <div class="row"><span>Fecha:</span><span>${fVenta}</span></div>
                <div class="row"><span>Cliente:</span><span>${cliNom}</span></div>
                <div class="row"><span>Doc/DNI:</span><span>${cliDoc}</span></div>
                <div class="row"><span>Dirección:</span><span>${cliDir}</span></div>
                <div class="line"></div>
                <div class="row"><strong>DESCRIPCIÓN</strong><strong>IMPORTE</strong></div>
                <div class="row"><span>Venta de Productos</span><span>$${tot}</span></div>
                <div class="line"></div>
                <div class="row total"><span>TOTAL PAGADO:</span><span>$${tot}</span></div>
                <div class="line"></div>
                <div class="center" style="margin-top: 15px;">
                    <p>Forma de Pago: Efectivo<br>¡Gracias por su preferencia!</p>
                </div>
                <script>
                    window.onload = function() {
                        window.print();
                        setTimeout(function(){ window.close(); }, 500);
                    };
                <\/script>
            </body>
            </html>
        `);
        ventana.document.close();
    } catch (err) {
        console.error(err);
        alert(`Error al imprimir comprobante: ${err.message}`);
    }
};

// 5. Inicializar la vista
document.addEventListener('DOMContentLoaded', () => {
    cargarVentas();

    const modalVenta = document.getElementById('modalVenta') || document.querySelector('.modal');
    const btnNuevaVenta = document.getElementById('btnNuevaVenta') || Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Nueva Venta'));
    const btnCerrarModal = document.getElementById('btnCerrarModalVenta') || modalVenta?.querySelector('.close');
    const btnCancelar = Array.from(modalVenta?.querySelectorAll('button') || []).find(b => b.textContent.trim().toLowerCase() === 'cancelar');
    const formVenta = document.getElementById('formNuevaVenta') || modalVenta?.querySelector('form');
    const selectCliente = document.getElementById('selectCliente') || formVenta?.querySelector('select');

    if (selectCliente) {
        const cargarComboClientes = async () => {
            try {
                let res = await fetch(`${API_CLIENTES_URL}/listar`, {
                    headers: {
                        'Accept': 'application/json',
                        'ngrok-skip-browser-warning': 'true'
                    }
                });

                if (res.status === 404) {
                    res = await fetch(API_CLIENTES_URL, {
                        headers: {
                            'Accept': 'application/json',
                            'ngrok-skip-browser-warning': 'true'
                        }
                    });
                }

                if (!res.ok) return;

                const clientes = await res.json();
                if (Array.isArray(clientes) && clientes.length > 0) {
                    selectCliente.innerHTML = '<option value="">-- Seleccionar Cliente --</option>';
                    clientes.forEach(c => {
                        const opt = document.createElement('option');
                        opt.value = c.idCliente || c.IdCliente;
                        opt.textContent = `${c.nombre || c.Nombre} (${c.documento || c.Documento})`;
                        selectCliente.appendChild(opt);
                    });
                }
            } catch (_) {}
        };

        cargarComboClientes();
    }

    const cerrar = () => { if (modalVenta) modalVenta.style.display = 'none'; };
    if (btnNuevaVenta && modalVenta) btnNuevaVenta.addEventListener('click', (e) => { e.preventDefault(); modalVenta.style.display = 'flex'; });
    if (btnCerrarModal) btnCerrarModal.addEventListener('click', cerrar);
    if (btnCancelar) btnCancelar.addEventListener('click', (e) => { e.preventDefault(); cerrar(); });

    if (formVenta) {
        formVenta.addEventListener('submit', async (e) => {
            e.preventDefault();
            const inputTotal = formVenta.querySelector('input[type="number"]') || document.getElementById('inputTotalVenta');
            const totalVal = inputTotal ? inputTotal.value.trim() : '';
            let idClienteVal = selectCliente ? selectCliente.value.trim() : '1';

            if (!totalVal || parseFloat(totalVal) <= 0) {
                alert('Ingresa un monto válido.');
                return;
            }

            let userId = 1;
            try {
                const sesion = JSON.parse(localStorage.getItem('usuarioSesion') || '{}');
                if (sesion.idUsuario || sesion.IdUsuario) {
                    userId = sesion.idUsuario || sesion.IdUsuario;
                }
            } catch (_) {
                userId = 1;
            }

            const nuevaVenta = {
                idCliente: parseInt(idClienteVal) || 1,
                IdCliente: parseInt(idClienteVal) || 1,
                idUsuario: parseInt(userId),
                IdUsuario: parseInt(userId),
                total: parseFloat(totalVal),
                Total: parseFloat(totalVal)
            };

            try {
                let res = await fetch(`${API_VENTAS_URL}/guardar`, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Accept': 'application/json',
                        'ngrok-skip-browser-warning': 'true'
                    },
                    body: JSON.stringify(nuevaVenta)
                });

                if (res.status === 404) {
                    res = await fetch(API_VENTAS_URL, {
                        method: 'POST',
                        headers: { 
                            'Content-Type': 'application/json',
                            'Accept': 'application/json',
                            'ngrok-skip-browser-warning': 'true'
                        },
                        body: JSON.stringify(nuevaVenta)
                    });
                }

                if (!res.ok) throw new Error(await res.text());

                alert('¡Venta registrada con éxito!');
                formVenta.reset();
                cerrar();
                cargarVentas();
            } catch (err) {
                alert(`Error al registrar: ${err.message}`);
            }
        });
    }
});