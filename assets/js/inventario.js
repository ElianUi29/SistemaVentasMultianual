// Variable global para evitar errores de scope y colisiones
window.API_PRODUCTOS_URL = window.API_PRODUCTOS_URL || 'https://pounce-chive-erratic.ngrok-free.dev/api/Productos';
var API_PRODUCTOS_URL = window.API_PRODUCTOS_URL;
var API_URL = window.API_PRODUCTOS_URL;

let listaProductosGlobal = [];

// Formateador de moneda en USD
const formatoUSD = (monto) => {
    return parseFloat(monto || 0).toLocaleString('en-US', {
        style: 'currency',
        currency: 'USD'
    });
};

// 1. Obtener y renderizar los productos desde la API
async function cargarProductos() {
    const tbody = document.getElementById('tablaInventarioBody');
    if (!tbody) return;

    try {
        let res = await fetch(`${API_PRODUCTOS_URL}/listar`, {
            headers: {
                'Accept': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            }
        });

        // Fallback si el controlador no usa /listar y responde directo en la raíz
        if (res.status === 404) {
            res = await fetch(API_PRODUCTOS_URL, {
                headers: {
                    'Accept': 'application/json',
                    'ngrok-skip-browser-warning': 'true'
                }
            });
        }

        if (!res.ok) throw new Error(`HTTP Error: ${res.status}`);

        listaProductosGlobal = await res.json();
        renderizarTablaProductos(listaProductosGlobal);
    } catch (error) {
        console.error('Error al cargar inventario:', error);
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; color:red; padding:15px;">Error al conectar con la API de SQL Server.</td></tr>`;
    }
}

// 2. Dibujar las filas en la tabla
function renderizarTablaProductos(productos) {
    const tbody = document.getElementById('tablaInventarioBody');
    if (!tbody) return;

    tbody.innerHTML = '';

    if (!Array.isArray(productos) || productos.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; padding:15px; color:#94a3b8;">No hay productos registrados en el inventario.</td></tr>`;
        return;
    }

    productos.forEach(p => {
        const id = p.idProducto || p.IdProducto;
        const codigo = p.codigo || p.Codigo || `PROD-${id}`;
        const nombre = p.nombre || p.Nombre || '-';
        const categoria = p.categoria || p.Categoria || 'General';
        const precio = formatoUSD(p.precio || p.Precio);
        const stock = parseInt(p.stock !== undefined ? p.stock : (p.Stock !== undefined ? p.Stock : 0));

        const esBajoStock = stock <= 5;
        const badgeColor = esBajoStock ? 'background:#fef3c7; color:#b45309;' : 'background:#dcfce7; color:#15803d;';
        const badgeTexto = esBajoStock ? 'Bajo Stock' : 'Disponible';

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td style="padding:12px 10px; font-weight:600;">${codigo}</td>
            <td style="padding:12px 10px; font-weight:500; color:#1e293b;">${nombre}</td>
            <td style="padding:12px 10px; color:#64748b;">${categoria}</td>
            <td style="padding:12px 10px; font-weight:600;">${precio}</td>
            <td style="padding:12px 10px;">${stock} unids</td>
            <td style="padding:12px 10px;">
                <span style="padding:4px 10px; border-radius:20px; font-size:0.8rem; font-weight:600; ${badgeColor}">
                    ${badgeTexto}
                </span>
            </td>
            <td style="padding:12px 10px; text-align:center;">
                <button type="button" onclick="window.prepararEditarProducto(${id})" style="background:none; border:1px solid #cbd5e1; border-radius:4px; padding:5px 8px; cursor:pointer; margin-right:5px;" title="Editar">
                    <i class="fas fa-edit" style="color:#0ea5e9;"></i>
                </button>
                <button type="button" onclick="window.eliminarProducto(${id}, '${nombre}')" style="background:#fee2e2; border:none; border-radius:4px; padding:5px 8px; cursor:pointer;" title="Eliminar">
                    <i class="fas fa-trash-alt" style="color:#ef4444;"></i>
                </button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

// 3. Preparar edición
window.prepararEditarProducto = async function(id) {
    try {
        const res = await fetch(`${API_PRODUCTOS_URL}/${id}`, {
            headers: {
                'Accept': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            }
        });
        if (!res.ok) throw new Error('No se pudo obtener el producto.');
        const p = await res.json();

        document.getElementById('modalProductoTitulo').textContent = 'Editar Producto';
        document.getElementById('prodId').value = p.idProducto || p.IdProducto;
        document.getElementById('prodNombre').value = p.nombre || p.Nombre;
        document.getElementById('prodCategoria').value = p.categoria || p.Categoria;
        document.getElementById('prodPrecio').value = p.precio || p.Precio;
        document.getElementById('prodStock').value = p.stock !== undefined ? p.stock : p.Stock;

        abrirModal();
    } catch (err) {
        alert(err.message);
    }
};

// 4. Eliminar producto físicamente
window.eliminarProducto = async function(id, nombre) {
    if (!confirm(`¿Estás seguro de eliminar el producto "${nombre}" de la base de datos?`)) return;

    try {
        let res = await fetch(`${API_PRODUCTOS_URL}/eliminar/${id}`, { 
            method: 'DELETE',
            headers: {
                'ngrok-skip-browser-warning': 'true'
            }
        });

        if (res.status === 404) {
            res = await fetch(`${API_PRODUCTOS_URL}/${id}`, {
                method: 'DELETE',
                headers: {
                    'ngrok-skip-browser-warning': 'true'
                }
            });
        }

        if (!res.ok) throw new Error(await res.text());

        alert('Producto eliminado exitosamente.');
        cargarProductos();
    } catch (err) {
        alert(`Error al eliminar: ${err.message}`);
    }
};

// 5. Control del modal y eventos
const modal = document.getElementById('modalNuevoProducto');

function abrirModal() {
    if (modal) modal.style.display = 'flex';
}

function cerrarModal() {
    if (modal) {
        modal.style.display = 'none';
        document.getElementById('formProductoModal')?.reset();
        document.getElementById('prodId').value = '0';
        document.getElementById('modalProductoTitulo').textContent = 'Registrar Nuevo Producto';
    }
}

document.addEventListener('DOMContentLoaded', () => {
    cargarProductos();

    // Botones modal
    document.getElementById('btnAbrirModalProd')?.addEventListener('click', () => {
        cerrarModal();
        abrirModal();
    });
    document.getElementById('btnCerrarXModal')?.addEventListener('click', cerrarModal);
    document.getElementById('btnCancelarModal')?.addEventListener('click', cerrarModal);

    // Cerrar al hacer clic fuera del recuadro
    window.addEventListener('click', (e) => {
        if (e.target === modal) cerrarModal();
    });

    // Búsqueda en vivo
    const inputFiltro = document.getElementById('filtroBuscarProducto');
    if (inputFiltro) {
        inputFiltro.addEventListener('input', (e) => {
            const term = e.target.value.toLowerCase().trim();
            const filtrados = listaProductosGlobal.filter(p => {
                const cod = (p.codigo || p.Codigo || '').toLowerCase();
                const nom = (p.nombre || p.Nombre || '').toLowerCase();
                const cat = (p.categoria || p.Categoria || '').toLowerCase();
                return cod.includes(term) || nom.includes(term) || cat.includes(term);
            });
            renderizarTablaProductos(filtrados);
        });
    }

    // Guardar (Nuevo o Actualizar)
    const form = document.getElementById('formProductoModal');
    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();

            const id = parseInt(document.getElementById('prodId').value) || 0;
            const nombre = document.getElementById('prodNombre').value.trim();
            const categoria = document.getElementById('prodCategoria').value;
            const precio = parseFloat(document.getElementById('prodPrecio').value);
            const stock = parseInt(document.getElementById('prodStock').value);

            if (!nombre || isNaN(precio) || isNaN(stock)) {
                alert('Por favor completa todos los campos con valores válidos.');
                return;
            }

            const data = {
                idProducto: id,
                IdProducto: id,
                nombre: nombre,
                Nombre: nombre,
                categoria: categoria,
                Categoria: categoria,
                precio: precio,
                Precio: precio,
                stock: stock,
                Stock: stock
            };

            const esEdicion = id > 0;
            let url = esEdicion ? `${API_PRODUCTOS_URL}/actualizar/${id}` : `${API_PRODUCTOS_URL}/guardar`;
            let metodo = esEdicion ? 'PUT' : 'POST';

            try {
                let res = await fetch(url, {
                    method: metodo,
                    headers: { 
                        'Content-Type': 'application/json',
                        'Accept': 'application/json',
                        'ngrok-skip-browser-warning': 'true'
                    },
                    body: JSON.stringify(data)
                });

                // Fallback de ruta directa si el backend usa /api/Productos/{id} o /api/Productos
                if (res.status === 404) {
                    const urlFallback = esEdicion ? `${API_PRODUCTOS_URL}/${id}` : API_PRODUCTOS_URL;
                    res = await fetch(urlFallback, {
                        method: metodo,
                        headers: { 
                            'Content-Type': 'application/json',
                            'Accept': 'application/json',
                            'ngrok-skip-browser-warning': 'true'
                        },
                        body: JSON.stringify(data)
                    });
                }

                if (!res.ok) throw new Error(await res.text());

                alert(esEdicion ? '¡Producto actualizado correctamente!' : '¡Producto guardado exitosamente en SQL Server!');
                cerrarModal();
                cargarProductos();
            } catch (err) {
                alert(`Error: ${err.message}`);
            }
        });
    }
});