const API_CAJA_URL = 'https://pounce-chive-erratic.ngrok-free.dev';
let listaProveedoresGlobal = [];

// 1. Obtener los proveedores desde la API y renderizarlos
async function cargarProveedores() {
    const tbody = document.getElementById('tbodyProveedores');
    if (!tbody) return;

    try {
        const respuesta = await fetch(`${API_PROVEEDORES_URL}/listar`);
        if (!respuesta.ok) throw new Error(`HTTP Error: ${respuesta.status}`);

        listaProveedoresGlobal = await respuesta.json();
        renderizarTablaProveedores(listaProveedoresGlobal);
    } catch (error) {
        console.error('Error al cargar proveedores:', error);
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; color:red; padding:15px;">Error al cargar proveedores de la base de datos.</td></tr>`;
    }
}

// 2. Pintar las filas en la tabla
function renderizarTablaProveedores(proveedores) {
    const tbody = document.getElementById('tbodyProveedores');
    if (!tbody) return;

    tbody.innerHTML = '';

    if (!Array.isArray(proveedores) || proveedores.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; padding:15px; color:#94a3b8;">No hay proveedores registrados.</td></tr>`;
        return;
    }

    proveedores.forEach(p => {
        const id = p.idProveedor || p.IdProveedor;
        const doc = p.documento || p.Documento;
        const razon = p.razonSocial || p.RazonSocial;
        const tel = p.telefono || p.Telefono || '-';
        const dir = p.direccion || p.Direccion || '-';

        const tr = document.createElement('tr');
        tr.style.borderBottom = '1px solid #f1f5f9';
        tr.innerHTML = `
            <td style="padding:12px 10px;"><strong>#${id}</strong></td>
            <td style="padding:12px 10px;"><span style="font-family:monospace; font-weight:600; color:#0369a1;">${doc}</span></td>
            <td style="padding:12px 10px; font-weight:600; color:#1e293b;">${razon}</td>
            <td style="padding:12px 10px;">${tel}</td>
            <td style="padding:12px 10px; color:#64748b;">${dir}</td>
            <td style="padding:12px 10px;"><span style="background:#dcfce7; color:#15803d; padding:4px 10px; border-radius:20px; font-size:0.8rem; font-weight:600;">Activo</span></td>
            <td style="padding:12px 10px; text-align:center;">
                <button type="button" class="btn btn-sm" onclick="window.eliminarProveedor(${id}, '${razon}')" style="background:#fee2e2; color:#b91c1c; border:none; padding:5px 10px; border-radius:4px; cursor:pointer;" title="Eliminar Proveedor">
                    <i class="fas fa-trash-alt"></i>
                </button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

// 3. Eliminar proveedor (baja lógica)
window.eliminarProveedor = async function(id, razon) {
    if (!confirm(`¿Estás seguro de eliminar al proveedor "${razon}"?`)) return;

    try {
        const res = await fetch(`${API_PROVEEDORES_URL}/eliminar/${id}`, { method: 'DELETE' });
        if (!res.ok) throw new Error(await res.text());

        alert('Proveedor eliminado correctamente.');
        cargarProveedores();
    } catch (err) {
        alert(`Error al eliminar: ${err.message}`);
    }
};

// 4. Inicializar modal y eventos
document.addEventListener('DOMContentLoaded', () => {
    cargarProveedores();

    const modal = document.getElementById('modalProveedor');
    const btnNuevo = document.getElementById('btnNuevoProveedor');
    const btnCerrar = document.getElementById('btnCerrarModalProv');
    const btnCancelar = document.getElementById('btnCancelarProv');
    const form = document.getElementById('formNuevoProveedor');
    const inputFiltro = document.getElementById('filtroProveedor');

    const abrir = () => { if (modal) modal.style.display = 'flex'; };
    const cerrar = () => { if (modal) modal.style.display = 'none'; };

    if (btnNuevo) btnNuevo.addEventListener('click', abrir);
    if (btnCerrar) btnCerrar.addEventListener('click', cerrar);
    if (btnCancelar) btnCancelar.addEventListener('click', cerrar);

    if (modal) {
        window.addEventListener('click', (e) => {
            if (e.target === modal) cerrar();
        });
    }

    // Filtrador en vivo por RUC o Razón Social
    if (inputFiltro) {
        inputFiltro.addEventListener('input', (e) => {
            const termino = e.target.value.toLowerCase().trim();
            const filtrados = listaProveedoresGlobal.filter(p => {
                const doc = (p.documento || p.Documento || '').toLowerCase();
                const razon = (p.razonSocial || p.RazonSocial || '').toLowerCase();
                return doc.includes(termino) || razon.includes(termino);
            });
            renderizarTablaProveedores(filtrados);
        });
    }

    // Guardar nuevo proveedor
    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();

            const doc = document.getElementById('provDoc')?.value.trim();
            const razon = document.getElementById('provRazon')?.value.trim();
            const tel = document.getElementById('provTel')?.value.trim();
            const dir = document.getElementById('provDir')?.value.trim();

            if (!doc || !razon) {
                alert('El Documento/RUC y la Razón Social son obligatorios.');
                return;
            }

            const nuevoProv = {
                documento: doc,
                razonSocial: razon,
                telefono: tel,
                direccion: dir
            };

            try {
                const res = await fetch(`${API_PROVEEDORES_URL}/guardar`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(nuevoProv)
                });

                if (!res.ok) throw new Error(await res.text());

                alert('¡Proveedor registrado exitosamente en SQL Server!');
                form.reset();
                cerrar();
                cargarProveedores();
            } catch (err) {
                alert(`Error al registrar: ${err.message}`);
            }
        });
    }
});