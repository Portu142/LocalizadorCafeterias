# Frontend · Localizador de Cafés

SPA en React 19 + TypeScript + Vite + Tailwind CSS.

```bash
npm ci
npm run dev      # http://localhost:5173
npm run build    # genera dist/
npm run preview  # sirve el build
npm run lint
```

Sin `VITE_API_URL`, la app funciona en modo demo con cafeterías reales de OpenStreetMap (Photon/Overpass) y guarda las reseñas en `localStorage`. Consulta el [README principal](../README.md) para la arquitectura, la API y el despliegue.
