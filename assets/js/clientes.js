const API_URL = 'http://localhost:5099/api/clientes';

// 1. Cargar clientes desde SQL Server
async function cargarClientes() {
    try {
        // Si tu endpoint en C# no tiene ("listar"), cámbialo a API_URL directo
        const respuesta = await fetch(`${API_URL}/listar`);
        
        if (!respuesta.ok) {
            // Intento de fallback si el controlador no usa la subruta /listar
            const respuestaFallback = await fetch(API_URL);
            if (!respuestaFallback.ok) {
                throw new Error(`HTTP Error: ${respuesta.status}`);
            }
            const clientesFallback = await respuestaFallback.json();
            renderizarTablaClientes(clientesFallback);
            return;
        }

        const clientes = await respuesta.json();
        renderizarTablaClientes(clientes);
    } catch (error) {
        console.error('Error al conectar con la API:', error);
    }
}

// 2. Dibujar clientes en la tabla
function renderizarTablaClientes(clientes) {
    const tbody = document.querySelector('tbody');
    if (!tbody) return;

    tbody.innerHTML = '';

    if (!Array.isArray(clientes) || clientes.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align:center; padding:15px;">No hay clientes registrados.</td></tr>`;
        return;
    }

    clientes.forEach(c => {
        // Maneja compatibilidad tanto para camelCase como PascalCase
        const documento = c.documento ?? c.Documento ?? '-';
        const nombre = c.nombre ?? c.Nombre ?? '-';
        const telefono = c.telefono ?? c.Telefono ?? '-';
        const direccion = c.direccion ?? c.Direccion ?? '-';

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${documento}</td>
            <td>${nombre}</td>
            <td>-</td>
            <td>${telefono}</td>
            <td>${direccion}</td>
        `;
        tbody.appendChild(tr);
    });
}

// 3. Configuración de la ventana Modal y Guardado
document.addEventListener('DOMContentLoaded', () => {
    cargarClientes();

    const modal = document.getElementById('modalCliente');
    const btnNuevo = document.getElementById('btnNuevoCliente') || document.querySelector('.btn-primary');
    const btnCerrar = document.getElementById('btnCerrarModal');
    const form = document.getElementById('formNuevoCliente');

    // Abrir modal
    if (btnNuevo && modal) {
        btnNuevo.addEventListener('click', (e) => {
            e.preventDefault();
            modal.style.display = 'flex';
        });
    }

    // Cerrar modal con botón cancelar / X
    if (btnCerrar && modal) {
        btnCerrar.addEventListener('click', () => {
            modal.style.display = 'none';
        });
    }

    // Cerrar modal al hacer clic en el fondo oscuro
    if (modal) {
        window.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.style.display = 'none';
            }
        });
    }

    // Guardar nuevo cliente con POST a la API
    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();

            const inputDoc = document.getElementById('nuevoDoc');
            const inputNombre = document.getElementById('nuevoNombre');
            const inputTel = document.getElementById('nuevoTelefono');
            const inputDir = document.getElementById('nuevaDireccion');

            const nuevoCliente = {
                documento: inputDoc ? inputDoc.value.trim() : '',
                nombre: inputNombre ? inputNombre.value.trim() : '',
                telefono: inputTel ? inputTel.value.trim() : '',
                direccion: inputDir ? inputDir.value.trim() : ''
            };

            if (!nuevoCliente.documento || !nuevoCliente.nombre) {
                alert('El documento y el nombre son obligatorios.');
                return;
            }

            try {
                // Si en C# tu POST es directo al root, cambia `${API_URL}/guardar` por API_URL
                const res = await fetch(`${API_URL}/guardar`, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    body: JSON.stringify(nuevoCliente)
                });

                if (!res.ok) {
                    const errorMsg = await res.text();
                    throw new Error(errorMsg || `Error del servidor (${res.status})`);
                }

                alert('¡Cliente guardado correctamente en SQL Server!');
                form.reset();
                if (modal) modal.style.display = 'none';
                cargarClientes(); // Recargar la tabla
            } catch (err) {
                console.error('Error al guardar cliente:', err);
                alert(`Error al guardar: ${err.message}`);
            }
        });
    }
});