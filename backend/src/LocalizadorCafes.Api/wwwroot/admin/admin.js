// Pide confirmación antes de enviar los formularios marcados con
// data-confirmar (borrar reseñas, cambiar permisos). Es un archivo aparte
// para que la CSP del panel no tenga que permitir JavaScript en línea.
document.addEventListener('submit', (event) => {
  const form = event.target;
  const message = form instanceof HTMLFormElement ? form.dataset.confirmar : undefined;

  if (message && !window.confirm(message)) {
    event.preventDefault();
  }
});

// Refresco automático de los bloques con data-refrescar («En vivo»): cada
// 5 s, solo con la pestaña visible. El servidor devuelve HTML ya escapado.
// Si la sesión caduca (redirección al inicio de sesión), se deja de pedir.
document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('[data-refrescar]').forEach((container) => {
    const url = container.getAttribute('data-refrescar');
    let stopped = false;

    const refresh = async () => {
      if (stopped || document.visibilityState !== 'visible') {
        return;
      }

      try {
        const response = await fetch(url, { credentials: 'same-origin', headers: { Accept: 'text/html' } });
        if (response.redirected || !response.ok) {
          stopped = response.redirected || response.status === 401 || response.status === 403;
          return;
        }
        container.innerHTML = await response.text();
      } catch {
        // Sin conexión momentánea: se reintenta en el siguiente ciclo.
      }
    };

    window.setInterval(refresh, 5000);
    document.addEventListener('visibilitychange', refresh);
  });
});
