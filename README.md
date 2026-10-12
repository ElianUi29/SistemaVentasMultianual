# Sistema de Ventas Multianual (2024-2026)

Sistema web integral para la gestión comercial, control de inventario, finanzas y análisis comparativo multianual de ventas.

## Estructura del Proyecto
- `index.html`: Dashboard ejecutivo con KPIs y gráficas (Chart.js).
- `login.html`: Portal de autenticación con diseño moderno.
- `pages/`: Módulos de ventas, inventario, clientes, proveedores, compras, caja, reportes, multianual, usuarios, roles y configuración.
- `assets/css/`: Estilos CSS modularizados, variables CSS y diseño responsive.
- `assets/js/`: Lógica de negocio interactiva, almacenamiento en LocalStorage y renderizado dinámico.
- `components/`: Componentes modulares reutilizables.
- `database/`: Scripts DDL y DML SQL para MySQL / PostgreSQL.
- `data/`: Datasets iniciales en formato JSON con información 2024-2026.

## Instrucciones para Visual Studio Code
1. Abre esta carpeta en Visual Studio Code: `Archivo > Abrir Carpeta...`
2. Instala la extensión **Live Server** (de Ritwick Dey).
3. Haz clic derecho sobre `index.html` o `login.html` y selecciona **"Open with Live Server"**.
4. ¡Listo! Puedes explorar todas las secciones, registrar ventas, ver el análisis comparativo multianual y descargar reportes.
