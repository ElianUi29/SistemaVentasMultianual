const API_AUTH_URL = 'https://pounce-chive-erratic.ngrok-free.dev';

document.addEventListener('DOMContentLoaded', () => {
    // Si ya existe una sesión activa, entra al Dashboard
    if (localStorage.getItem('usuarioSesion')) {
        window.location.href = 'index.html';
        return;
    }

    const formLogin = document.getElementById('formLogin');
    const formRegistro = document.getElementById('formRegistro');
    const btnIrRegistro = document.getElementById('btnIrRegistro');
    const btnIrLogin = document.getElementById('btnIrLogin');
    const tituloAuth = document.getElementById('tituloAuth');
    const subtituloAuth = document.getElementById('subtituloAuth');
    const alertaBox = document.getElementById('alertaBox');

    function mostrarAlerta(mensaje, esExito = false) {
        if (!alertaBox) return;
        alertaBox.textContent = mensaje;
        alertaBox.className = esExito ? 'alert-msg alert-success' : 'alert-msg alert-danger';
        alertaBox.style.display = 'block';
    }

    function ocultarAlerta() {
        if (alertaBox) alertaBox.style.display = 'none';
    }

    // Alternar a Registro
    if (btnIrRegistro) {
        btnIrRegistro.addEventListener('click', (e) => {
            e.preventDefault();
            ocultarAlerta();
            formLogin.style.display = 'none';
            formRegistro.style.display = 'block';
            tituloAuth.textContent = 'Crear Usuario';
            subtituloAuth.textContent = 'Registra un nuevo usuario en SQL Server';
        });
    }

    // Alternar a Login
    if (btnIrLogin) {
        btnIrLogin.addEventListener('click', (e) => {
            e.preventDefault();
            ocultarAlerta();
            formRegistro.style.display = 'none';
            formLogin.style.display = 'block';
            tituloAuth.textContent = 'Iniciar Sesión';
            subtituloAuth.textContent = 'Ingresa a tu cuenta de MultiVentas';
        });
    }

    // 1. INICIAR SESIÓN
    if (formLogin) {
        formLogin.addEventListener('submit', async (e) => {
            e.preventDefault();
            ocultarAlerta();

            const correo = document.getElementById('loginCorreo')?.value.trim();
            const clave = document.getElementById('loginClave')?.value.trim();

            if (!correo || !clave) {
                mostrarAlerta('Ingresa tu correo y contraseña.');
                return;
            }

            try {
                const res = await fetch(`${API_AUTH_URL}/login`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ correo, clave })
                });

                const data = await res.json();

                if (!res.ok) {
                    mostrarAlerta(data.mensaje || 'Error al iniciar sesión.');
                    return;
                }

                // Guardar usuario autenticado
                localStorage.setItem('usuarioSesion', JSON.stringify(data.usuario));

                mostrarAlerta('¡Acceso concedido! Entrando...', true);
                setTimeout(() => {
                    window.location.href = 'index.html';
                }, 800);

            } catch (err) {
                console.error(err);
                mostrarAlerta('No se pudo conectar con el servidor.');
            }
        });
    }

    // 2. CREAR NUEVO USUARIO (REGISTRO EN BD)
    if (formRegistro) {
        formRegistro.addEventListener('submit', async (e) => {
            e.preventDefault();
            ocultarAlerta();

            const nombre = document.getElementById('regNombre')?.value.trim();
            const correo = document.getElementById('regCorreo')?.value.trim();
            const clave = document.getElementById('regClave')?.value.trim();
            const rol = document.getElementById('regRol')?.value;

            if (!nombre || !correo || !clave) {
                mostrarAlerta('Completa todos los campos obligatorios.');
                return;
            }

            const nuevoUsuario = {
                nombreCompleto: nombre,
                correo: correo,
                clave: clave,
                rol: rol
            };

            try {
                const res = await fetch(`${API_AUTH_URL}/registro`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(nuevoUsuario)
                });

                const data = await res.json();

                if (!res.ok) {
                    mostrarAlerta(data.mensaje || 'Error al registrar el usuario.');
                    return;
                }

                mostrarAlerta(data.mensaje, true);
                formRegistro.reset();

                // Regresar a la vista de login y precargar el correo registrado
                setTimeout(() => {
                    btnIrLogin.click();
                    document.getElementById('loginCorreo').value = correo;
                    document.getElementById('loginClave').focus();
                    mostrarAlerta('¡Usuario registrado en SQL Server! Ahora ingresa tu contraseña.', true);
                }, 1500);

            } catch (err) {
                console.error(err);
                mostrarAlerta('No se pudo conectar con el servidor para registrar.');
            }
        });
    }
});