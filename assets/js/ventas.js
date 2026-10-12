const API_VENTAS_URL = 'http://localhost:5099/api/ventas';
const API_CLIENTES_URL = 'http://localhost:5099/api/clientes';

// 1. Cargar la lista de ventas desde SQL Server
async function cargarVentas() {
    const tbody = document.querySelector('tbody');
    if (!tbody) return;

    try {
        const respuesta = await fetch(`${API_VENTAS_URL}/listar`);
        if (!respuesta.ok) throw new Error(`HTTP Error: ${respuesta.status}`);

        const ventas = await respuesta.json();
        renderizarTablaVentas(ventas);
    } catch (error) {
        console.error('Error al conectar con la API de ventas:', error);
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; color:red; padding:15px;">Error al cargar ventas desde la base de datos.</td></tr>`;
    }
}

// 2. Renderizar filas y vincular botones
function renderizarTablaVentas(ventas) {
    const tbody = document.querySelector('tbody');
    if (!tbody) return;

    tbody.innerHTML = '';

    if (!Array.isArray(ventas) || ventas.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; padding:15px;">No hay comprobantes de venta registrados.</td></tr>`;
        return;
    }

    ventas.forEach(v => {
        const idVenta = v.idVenta || v.IdVenta;
        const codigo = v.numeroComprobante || `VNT-${idVenta}`;
        const fecha = v.fechaVenta || '-';
        const cliente = v.cliente || 'Consumidor Final';
        const total = parseFloat(v.total || 0).toLocaleString('en-US', { style: 'currency', currency: 'USD' });

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
    console.log('Consultando detalle para ID:', idVenta);
    try {
        const res = await fetch(`${API_VENTAS_URL}/detalle/${idVenta}`);
        if (!res.ok) throw new Error('No se pudo obtener el detalle de la venta.');

        const data = await res.json();
        alert(
            `==============================\n` +
            `       DETALLE DE VENTA       \n` +
            `==============================\n` +
            `N° Comprobante: ${data.numeroComprobante}\n` +
            `Fecha:         ${data.fechaVenta}\n` +
            `Cliente:       ${data.cliente}\n` +
            `Doc/DNI:       ${data.documento}\n` +
            `Teléfono:      ${data.telefono}\n` +
            `Dirección:     ${data.direccion}\n` +
            `------------------------------\n` +
            `TOTAL:         $${parseFloat(data.total).toFixed(2)}\n` +
            `Estado:        Completada\n` +
            `==============================`
        );
    } catch (err) {
        console.error(err);
        alert(`Error al ver detalle: ${err.message}`);
    }
};

// 4. Función global: IMPRIMIR (Impresora)
window.imprimirComprobante = async function(idVenta) {
    console.log('Imprimiendo comprobante para ID:', idVenta);
    try {
        const res = await fetch(`${API_VENTAS_URL}/detalle/${idVenta}`);
        if (!res.ok) throw new Error('No se pudo obtener el comprobante.');

        const data = await res.json();

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
                <title>Comprobante - ${data.numeroComprobante}</title>
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
                    <p style="margin: 3px 0;">${data.numeroComprobante}</p>
                </div>
                <div class="line"></div>
                <div class="row"><span>Fecha:</span><span>${data.fechaVenta}</span></div>
                <div class="row"><span>Cliente:</span><span>${data.cliente}</span></div>
                <div class="row"><span>Doc/DNI:</span><span>${data.documento}</span></div>
                <div class="row"><span>Dirección:</span><span>${data.direccion}</span></div>
                <div class="line"></div>
                <div class="row"><strong>DESCRIPCIÓN</strong><strong>IMPORTE</strong></div>
                <div class="row"><span>Venta de Productos</span><span>$${parseFloat(data.total).toFixed(2)}</span></div>
                <div class="line"></div>
                <div class="row total"><span>TOTAL PAGADO:</span><span>$${parseFloat(data.total).toFixed(2)}</span></div>
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
        fetch(`${API_CLIENTES_URL}/listar`)
            .then(res => res.json())
            .then(clientes => {
                if (Array.isArray(clientes) && clientes.length > 0) {
                    selectCliente.innerHTML = '';
                    clientes.forEach(c => {
                        const opt = document.createElement('option');
                        opt.value = c.idCliente || c.IdCliente;
                        opt.textContent = `${c.nombre || c.Nombre} (${c.documento || c.Documento})`;
                        selectCliente.appendChild(opt);
                    });
                }
            })
            .catch(() => {});
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

            const nuevaVenta = {
                idCliente: parseInt(idClienteVal) || 1,
                idUsuario: 1,
                total: parseFloat(totalVal)
            };

            try {
                const res = await fetch(`${API_VENTAS_URL}/guardar`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(nuevaVenta)
                });
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