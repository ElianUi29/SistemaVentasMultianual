// assets/js/dashboard.js

document.addEventListener('DOMContentLoaded', () => {
  // Función para obtener cualquier variable definida en el CSS
  const getCssVar = (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();

  // 1. Gráfico Multianual de Ventas
  const ctxVentas = document.getElementById('ventasChart');
  if (ctxVentas) {
    new Chart(ctxVentas, {
      type: 'line',
      data: {
        labels: ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'],
        datasets: [
          {
            label: '2024 ($)',
            data: [12000, 15000, 14000, 18000, 17000, 21000, 23000, 22000, 25000, 27000, 31000, 35000],
            borderColor: getCssVar('--chart-color-2024'),
            backgroundColor: getCssVar('--chart-bg-2024'),
            tension: 0.3,
            fill: true
          },
          {
            label: '2025 ($)',
            data: [18000, 20000, 22000, 25000, 27000, 29000, 31000, 34000, 37000, 40000, 43000, 48000],
            borderColor: getCssVar('--chart-color-2025'),
            backgroundColor: getCssVar('--chart-bg-2025'),
            tension: 0.3,
            fill: true
          },
          {
            label: '2026 ($ Proyectado/Actual)',
            data: [26000, 29000, 33000, 38000, 42000, 47000, 52000, 56000, 61000, 65000, 71000, 78000],
            borderColor: getCssVar('--chart-color-2026'),
            backgroundColor: getCssVar('--chart-bg-2026'),
            tension: 0.3,
            fill: true
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'top' }
        },
        scales: {
          y: {
            beginAtZero: false,
            grid: { color: getCssVar('--chart-grid-line') }
          },
          x: {
            grid: { display: false }
          }
        }
      }
    });
  }

  // 2. Gráfico Donut de Categorías
  const ctxCat = document.getElementById('categoriasChart');
  if (ctxCat) {
    new Chart(ctxCat, {
      type: 'doughnut',
      data: {
        labels: ['Electrónica', 'Laptops & PCs', 'Accesorios', 'Software', 'Servicios'],
        datasets: [{
          data: [35, 25, 20, 12, 8],
          backgroundColor: [
            getCssVar('--chart-cat-elec'),
            getCssVar('--chart-cat-pcs'),
            getCssVar('--chart-cat-acc'),
            getCssVar('--chart-cat-soft'),
            getCssVar('--chart-cat-serv')
          ]
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'bottom' }
        }
      }
    });
  }
});