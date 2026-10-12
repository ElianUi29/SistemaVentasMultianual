const API_CAJA_URL = 'http://localhost:5099/api/caja';

const formatoUSD = (monto) => {
    return parseFloat(monto || 0).toLocaleString('en-US', {
        style: 'currency',
        currency: 'USD'
    });
};

// 1. Cargar las 4 tarjetas de resumen
async function cargarResumenCaja() {
    try {
        const respuesta = await fetch(`${API_CAJA_URL}/resumen`);
        if (!respuesta.ok) throw new Error(`HTTP Error: ${respuesta.status}`);

        const data = await respuesta.json();

        document.getElementById('valSaldoInicial').textContent = formatoUSD(data.saldoInicial);
        document.getElementById('valIngresos').textContent = formatoUSD(data.ingresos);
        document.getElementById('valEgresos').textContent = formatoUSD(data.egresos);
        document.getElementById('valTotalEfectivo').textContent = formatoUSD(data.totalEfectivo);
    } catch (error) {
        console.error('Error al cargar resumen de caja:', error);
    }
}

// 2. Cargar tabla de movimientos
async function cargarMovimientos() {
    const tbody = document.getElementById('tbodyMovimientos');
    if (!tbody) return;

    try {
        const res = await fetch(`${API_CAJA_URL}/movimientos`);
        if (!res.ok) throw new Error(`HTTP Error: ${res.status}`);

        const movimientos = await res.json();
        tbody.innerHTML = '';

        if (!Array.isArray(movimientos) || movimientos.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" style="text-align:center; padding:15px; color:#94a3b8;">No hay ingresos o egresos registrados en esta caja todavía.</td></tr>`;
            return;
        }

        movimientos.forEach(m => {
            const esIngreso = m.tipo === 'Ingreso';
            const colorTipo = esIngreso ? 'background:#dcfce7; color:#15803d;' : 'background:#fee2e2; color:#b91c1c;';
            const signo = esIngreso ? '+' : '-';
            const colorMonto = esIngreso ? '#15803d' : '#b91c1c';

            const tr = document.createElement('tr');
            tr.style.borderBottom = '1px solid #f1f5f9';
            tr.innerHTML = `
                <td style="padding:10px;">#${m.idMovimiento}</td>
                <td style="padding:10px;">${m.fecha}</td>
                <td style="padding:10px;"><span style="padding:3px 10px; border-radius:12px; font-weight:600; font-size:0.8rem; ${colorTipo}">${m.tipo}</span></td>
                <td style="padding:10px;">${m.concepto}</td>
                <td style="padding:10px; text-align:right; font-weight:bold; color:${colorMonto};">${signo}${formatoUSD(m.monto)}</td>
            `;
            tbody.appendChild(tr);
        });
    } catch (err) {
        console.error('Error al cargar movimientos:', err);
    }
}

// 3. Registrar un ingreso o un egreso
async function agregarMovimiento(tipo) {
    const concepto = prompt(`Ingresa el concepto del ${tipo}:`, tipo === 'Ingreso' ? 'Venta extra en mostrador' : 'Pago de servicio / compra menor');
    if (!concepto || concepto.trim() === '') return;

    const montoStr = prompt(`Ingresa el monto del ${tipo} ($):`, '50.00');
    if (!montoStr) return;

    const monto = parseFloat(montoStr);
    if (isNaN(monto) || monto <= 0) {
        alert('Ingresa un monto numérico válido mayor a 0.');
        return;
    }

    try {
        const res = await fetch(`${API_CAJA_URL}/movimiento`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ tipo: tipo, concepto: concepto.trim(), monto: monto })
        });

        if (!res.ok) throw new Error(await res.text());

        alert(`¡${tipo} de $${monto.toFixed(2)} registrado correctamente!`);
        cargarResumenCaja();
        cargarMovimientos();
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}

// 4. Inicializar eventos
document.addEventListener('DOMContentLoaded', () => {
    cargarResumenCaja();
    cargarMovimientos();

    // Botones de movimientos
    document.getElementById('btnNuevoIngreso')?.addEventListener('click', () => agregarMovimiento('Ingreso'));
    document.getElementById('btnNuevoGasto')?.addEventListener('click', () => agregarMovimiento('Egreso'));

    // Botón Apertura
    document.getElementById('btnAperturaCaja')?.addEventListener('click', async () => {
        const entrada = prompt('Ingresa el monto del Saldo Inicial ($):', '350.00');
        if (!entrada) return;
        const saldo = parseFloat(entrada);
        if (isNaN(saldo) || saldo < 0) return alert('Monto inválido.');

        try {
            const res = await fetch(`${API_CAJA_URL}/apertura`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ saldoInicial: saldo, idUsuario: 1 })
            });
            if (!res.ok) throw new Error(await res.text());
            alert('¡Caja abierta exitosamente!');
            cargarResumenCaja();
            cargarMovimientos();
        } catch (err) {
            alert(`Error: ${err.message}`);
        }
    });

    // Botón Corte de Caja
    document.getElementById('btnCorteCaja')?.addEventListener('click', async () => {
        if (!confirm('¿Deseas realizar el Corte / Cierre de la Caja actual?')) return;
        try {
            const res = await fetch(`${API_CAJA_URL}/corte`, { method: 'POST' });
            if (!res.ok) throw new Error(await res.text());
            alert('¡Caja cerrada exitosamente!');
            cargarResumenCaja();
            cargarMovimientos();
        } catch (err) {
            alert(`Error: ${err.message}`);
        }
    });
});