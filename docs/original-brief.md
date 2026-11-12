# Localizador de Cafés

## Descripción

Aplicación web estática para encontrar cafeterías cercanas, consultar sus horarios, menús y reseñas.

## Características

- Búsqueda de cafeterías por nombre, barrio o dirección
- Listado interactivo con valoraciones
- Mapa integrado con Leaflet y OpenStreetMap
- Ficha de cada cafetería con horarios, teléfono y menú
- Reseñas de clientes y formulario para añadir nuevas reseñas (guardadas en el navegador)

## Estructura del Proyecto

```
Localizador_cafes/
├── index.html
├── Localizador de cafés.md
├── css/
│   └── styles.css
├── js/
│   └── app.js
├── data/
│   └── cafes.js
├── assets/
│   ├── icons/
│   └── images/
└── docs/
```

## Instrucciones de Uso

1. Abre `index.html` en un navegador moderno.
2. Usa el buscador para filtrar por nombre, barrio o dirección.
3. Haz clic en una cafetería del listado o en un marcador del mapa para ver su ficha.
4. Añade reseñas desde la ficha de cada cafetería; se guardan en `localStorage`.

> Nota: el mapa usa OpenStreetMap, por lo que necesita conexión a internet para cargar los mosaicos.

## Tecnologías

- HTML5
- CSS3
- JavaScript (ES6+)
- Leaflet 1.9.4
- OpenStreetMap

## Contacto

Para más información, contactar al equipo de desarrollo.
