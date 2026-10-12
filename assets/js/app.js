// App Global Logic
document.addEventListener('DOMContentLoaded', () => {
  // 1. Toggle Sidebar (Menú móvil / responsivo)
  const toggleBtn = document.querySelector('.toggle-sidebar-btn');
  const sidebar = document.querySelector('.sidebar');
  if (toggleBtn && sidebar) {
    toggleBtn.addEventListener('click', () => {
      sidebar.classList.toggle('active');
    });
  }

  // 2. Resaltar enlace activo en la barra lateral
  const currentPath = window.location.pathname.split('/').pop() || 'index.html';
  document.querySelectorAll('.sidebar-menu a').forEach(link => {
    const rawHref = link.getAttribute('href');
    if (rawHref && rawHref !== '#') {
      const href = rawHref.split('/').pop();
      if (href === currentPath) {
        link.classList.add('active');
      }
    }
  });

  // 3. Control de Cerrar Sesión (Salida al Login)
  const btnSalir = document.querySelector('a[href*="login.html"], .sidebar-footer a');
  if (btnSalir) {
    btnSalir.addEventListener('click', (e) => {
      e.preventDefault();

      if (confirm('¿Estás seguro de que deseas cerrar sesión?')) {
        // Eliminar credenciales y sesión guardada
        localStorage.removeItem('usuarioSesion');
        sessionStorage.clear();

        // Redirigir a login considerando si la página actual está en subcarpeta (pages/)
        const esSubcarpeta = window.location.pathname.includes('/pages/');
        window.location.href = esSubcarpeta ? '../login.html' : 'login.html';
      }
    });
  }
});

// Modal Helpers (compatibles con style.display y clases CSS)
function openModal(id) {
  const m = document.getElementById(id);
  if (m) {
    m.classList.add('active');
    m.style.display = 'flex';
  }
}

function closeModal(id) {
  const m = document.getElementById(id);
  if (m) {
    m.classList.remove('active');
    m.style.display = 'none';
  }
}