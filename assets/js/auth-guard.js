(function() {
    const sesion = localStorage.getItem('usuarioSesion');

    // 1. Si no hay sesión iniciada, expulsar al login
    if (!sesion) {
        alert('Debes iniciar sesión para acceder al sistema.');
        const esSubcarpeta = window.location.pathname.includes('/pages/');
        window.location.href = esSubcarpeta ? '../login.html' : 'login.html';
        return;
    }

    const usuario = JSON.parse(sesion);

    // 2. Colocar nombre real, rol y avatar en la barra superior
    document.addEventListener('DOMContentLoaded', () => {
        const spanNombre = document.querySelector('.user-name');
        const spanRol = document.querySelector('.user-role');
        const userAvatar = document.querySelector('.user-avatar');

        if (spanNombre) spanNombre.textContent = usuario.nombreCompleto;
        if (spanRol) spanRol.textContent = usuario.rol;
        if (userAvatar && usuario.nombreCompleto) {
            userAvatar.textContent = usuario.nombreCompleto.substring(0, 2).toUpperCase();
        }

        // 3. Control de Permisos por Rol:
        // Si el usuario es "Vendedor", ocultar accesos restringidos (Usuarios, Roles, Configuración)
        if (usuario.rol === 'Vendedor') {
            const itemsRestringidos = document.querySelectorAll('a[href*="usuarios.html"], a[href*="roles.html"], a[href*="configuracion.html"]');
            itemsRestringidos.forEach(item => {
                const li = item.closest('li');
                if (li) li.style.display = 'none';
            });
        }

        // 4. Cerrar Sesión
        const btnSalir = document.querySelector('a[href*="login.html"]');
        if (btnSalir) {
            btnSalir.addEventListener('click', (e) => {
                e.preventDefault();
                if (confirm('¿Deseas cerrar sesión?')) {
                    localStorage.removeItem('usuarioSesion');
                    const esSubcarpeta = window.location.pathname.includes('/pages/');
                    window.location.href = esSubcarpeta ? '../login.html' : 'login.html';
                }
            });
        }
    });
})();