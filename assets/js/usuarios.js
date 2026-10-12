// Usuarios Logic
const usuariosData = [
  { id: 1, nombre: 'Administrador General', user: 'admin@sistema.com', rol: 'SuperAdmin', estado: 'Activo' },
  { id: 2, nombre: 'Valeria Gómez', user: 'valeria.ventas@sistema.com', rol: 'Vendedor', estado: 'Activo' },
  { id: 3, nombre: 'Ricardo Morales', user: 'ricardo.almacen@sistema.com', rol: 'Almacenero', estado: 'Inactivo' }
];

function renderUsuarios() {
  const tbody = document.getElementById('tablaUsuariosBody');
  if (!tbody) return;
  tbody.innerHTML = usuariosData.map(u => `
    <tr>
      <td>#${u.id}</td>
      <td><strong>${u.nombre}</strong></td>
      <td>${u.user}</td>
      <td><span class="badge badge-info">${u.rol}</span></td>
      <td><span class="badge ${u.estado === 'Activo' ? 'badge-success' : 'badge-danger'}">${u.estado}</span></td>
      <td>
        <button class="btn btn-secondary" style="padding: 4px 8px;"><i class="fas fa-user-edit"></i></button>
      </td>
    </tr>
  `).join('');
}

document.addEventListener('DOMContentLoaded', renderUsuarios);
