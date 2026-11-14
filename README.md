# ☕ Localizador de Cafés

Buscador de cafeterías de especialidad cerca de ti. Proyecto **full-stack** pensado como pieza de portafolio: geolocalización automática para ordenar por cercanía, mapa interactivo, filtros por barrio, fichas con menú y horarios, favoritos y reseñas.
**Demo:** _pendiente de publicar en GitHub Pages_ · **API:** ASP.NET Core 8 + PostgreSQL/PostGIS (en desarrollo local)

![Vista de escritorio del Localizador de Cafés](docs/screenshots/desktop.png)

## Cerca de mí

Al cargar la página se solicita permiso de ubicación automáticamente. Si lo concedes, la app busca cafeterías reales de OpenStreetMap en un radio de 5 km, las ordena de menor a mayor distancia y marca tu posición en el mapa. Si lo deniegas (o el navegador bloquea la petición automática), aparece un aviso con el botón «Permitir ubicación»: al concederlo, la app se recentra sola en tu zona sin recargar. Mientras tanto, enseña cafeterías de Madrid como referencia.

Los **favoritos no dependen de la ubicación**: se guarda la ficha del café en `localStorage`, así que siguen apareciendo aunque te muevas a otra ciudad o cambies de zona.

![Localizador de Cafés con la ubicación activa](docs/screenshots/location.png)

---

## Sobre el proyecto

El objetivo era construir algo más que una landing: una aplicación real con separación de capas, consultas geoespaciales, validación de datos, estados de carga y error, accesibilidad y un despliegue reproducible.

- **Frontend** React 19 + TypeScript + Vite + Tailwind CSS, con React Query para el estado del servidor.
- **Backend** ASP.NET Core 8 con arquitectura por capas (`Domain`, `Application`, `Infrastructure`, `Api`).
- **Base de datos** PostgreSQL 16 + PostGIS, con columna `geography(Point)` e índice GIST para búsquedas por cercanía.
- **Demo estática** en GitHub Pages: si `VITE_API_URL` está vacío, el frontend consulta cafeterías reales de OpenStreetMap (Photon y Overpass) y guarda reseñas/favoritos en `localStorage`; si las APIs públicas fallan, usa los datos de ejemplo de Madrid.

### Funcionalidades

- **Cerca de mí**: geolocalización del dispositivo con haversine, radio de 5 km y orden por distancia; estados de permiso concedido, denegado, no disponible y timeout. La ubicación se pide automáticamente al cargar.
- Búsqueda por nombre, dirección o barrio, insensible a mayúsculas y acentos.
- Filtro por barrio y vista de favoritos persistentes e independientes de la ubicación.
- Mapa Leaflet/OpenStreetMap con marcadores sincronizados con el listado, punto azul con tu posición y encuadre automático de los resultados.
- Ficha de cafetería: distancia a tu posición, horario, teléfono, enlace a Google Maps, menú y reseñas.
- Formulario de reseñas con validación en cliente y servidor, contador de caracteres y estados de éxito/error.
- Valoración media y número de reseñas actualizados al publicar.
- Diseño responsive (móvil, tablet, escritorio) y navegación por teclado.
- SEO técnico: canonical, Open Graph, Twitter Card, JSON-LD, `robots.txt`, `sitemap.xml` y favicon.
- Estados de carga (skeletons), error con reintento y listas vacías con acción de recuperación.

### Vista móvil

![Vista móvil del Localizador de Cafés](docs/screenshots/mobile.png)

### Stack

| Capa | Tecnología |
| --- | --- |
| Frontend | React 19, TypeScript, Vite 8, Tailwind CSS 3.4, React Query 5 |
| Mapa | Leaflet 1.9 + React Leaflet 5 + OpenStreetMap |
| Backend | .NET 8, ASP.NET Core Web API, EF Core 8, FluentValidation |
| Base de datos | PostgreSQL 16 + PostGIS 3.4 |
| Observabilidad | Serilog, health checks |
| Pruebas | xUnit + FluentValidation.TestHelper |
| Infra | Docker Compose, nginx, GitHub Actions, GitHub Pages |

### Arquitectura

```text
┌──────────────────────────┐        ┌──────────────────────────┐
│  React + Vite (SPA)      │  HTTP  │  ASP.NET Core Web API    │
│  React Query · Leaflet   │ ─────► │  Controllers · Services  │
│  Tailwind · localStorage │        │  FluentValidation        │
└──────────────────────────┘        └────────────┬─────────────┘
                                                  │ EF Core
                                                  ▼
                                             ┌──────────────────────────┐
                                             │  PostgreSQL + PostGIS    │
                                             │  geography(Point) · GIST │
                                             └──────────────────────────┘
```

El backend sigue una arquitectura limpia en cuatro proyectos:

- `Domain`: entidades (`Cafe`, `MenuItem`, `Review`) y reglas de negocio.
- `Application`: DTOs, `CafeService`, validadores e interfaces de repositorio.
- `Infrastructure`: `AppDbContext`, configuraciones EF Core, migraciones, repositorio y seed.
- `Api`: controladores, middleware de errores y cabeceras de seguridad, rate limiting, Swagger, health checks.

### Puesta en marcha

#### 1. Frontend en modo demo (sin backend)

```bash
cd frontend
npm ci
npm run dev
```

Abre `http://localhost:5173`. Sin `VITE_API_URL`, la app consulta cafeterías reales de OpenStreetMap cerca de tu ubicación (con datos de ejemplo de Madrid como respaldo); las reseñas y los favoritos se guardan en `localStorage`.

Para probar el build de producción:

```bash
npm run build
npm run preview
```

#### 2. Backend (arranca sin PostgreSQL)

```bash
cd backend
dotnet run --project src/LocalizadorCafes.Api
```

- API: `http://localhost:5067`
- Swagger (solo Development): `http://localhost:5067/swagger`
- Health check: `http://localhost:5067/health`

Si no hay PostgreSQL disponible, la API **arranca en modo demo** con las 6 cafeterías de ejemplo en memoria: todos los endpoints funcionan y las reseñas nuevas se pierden al reiniciar. El aviso aparece en la consola al iniciar.

##### Con PostgreSQL + PostGIS

Crea la base de datos y habilita PostGIS:

```sql
CREATE DATABASE localizador_cafes;
\c localizador_cafes
CREATE EXTENSION IF NOT EXISTS postgis;
```

Ajusta la cadena de conexión en `backend/src/LocalizadorCafes.Api/appsettings.json` o con una variable de entorno:

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=localizador_cafes;Username=postgres;Password=postgres"
```

Al arrancar aplica las migraciones y el seed automáticamente si `Database:AutoMigrate` es `true`. Si prefieres que falle en vez de usar el modo demo, define `Database__AllowInMemoryFallback=false`.

Después arranca el frontend apuntando a la API:

```bash
cd frontend
# frontend/.env.local
VITE_API_URL=http://localhost:5067
npm run dev
```

#### 3. Docker Compose (todo en uno)

```bash
cp .env.example .env
docker compose up --build
```

- Frontend: `http://localhost:8080`
- API: `http://localhost:5080`
- PostgreSQL/PostGIS: `localhost:5432`

El servicio `db` espera a estar sano antes de arrancar la API; `Database__AutoMigrate=true` crea el esquema y carga los datos de ejemplo.

### Variables de entorno

#### Frontend (`frontend/.env.example`)

| Variable | Por defecto | Descripción |
| --- | --- | --- |
| `VITE_API_URL` | vacío | URL de la API. Vacío = modo demo con datos reales de OpenStreetMap. |
| `VITE_SITE_URL` | `https://example.github.io/localizador-cafes/` | URL pública para canonical, Open Graph y sitemap. |

#### Backend

| Variable | Por defecto | Descripción |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | `Host=localhost;...` | Cadena de conexión a PostgreSQL. |
| `Database__AutoMigrate` | `true` | Aplica migraciones y seed al arrancar. |
| `Database__AllowInMemoryFallback` | `true` | Si no hay PostgreSQL, arranca en modo demo con datos en memoria. |
| `Cors__AllowedOrigins__0..n` | `localhost:5173`, `localhost:4173`, `127.0.0.1:5173`, `127.0.0.1:4173` | Orígenes permitidos. |
| `ReverseProxy__TrustForwardedHeaders` | `false` | Activar detrás de un proxy de confianza. |

### API

| Método | Ruta | Descripción |
| --- | --- | --- |
| `GET` | `/api/cafes` | Lista/busca cafeterías. Parámetros: `search`, `lat`, `lng`, `radiusKm`. |
| `GET` | `/api/cafes/{id}` | Ficha completa con menú y reseñas. |
| `POST` | `/api/cafes/{id}/reviews` | Añade una reseña. Limitado a 8 peticiones/minuto por IP. |
| `GET` | `/health` | Estado de la API y la base de datos. |

Ejemplos:

```bash
curl "http://localhost:5067/api/cafes?search=malasa%C3%B1a"
curl "http://localhost:5067/api/cafes?lat=40.4168&lng=-3.7038&radiusKm=3"
curl -X POST "http://localhost:5067/api/cafes/11111111-1111-1111-1111-111111111111/reviews" \
  -H "Content-Type: application/json" \
  -d '{"author":"Ana","rating":5,"comment":"Un café excelente y un ambiente muy tranquilo."}'
```

La validación devuelve `400` con `ValidationProblemDetails`; los parámetros geográficos se comprueban antes de llegar a la base de datos.

### Datos de ejemplo

El seed incluye **6 cafeterías** en 4 barrios de Madrid (Huertas, Malasaña, Palacio, Conde Duque), con menús, horarios y **12 reseñas** iniciales. Los mismos datos alimentan el modo demo en memoria.

### Calidad y pruebas

#### Backend

```bash
cd backend
dotnet test
```

- 18 pruebas unitarias: validador de reseñas (autor, longitudes, rating y comentario) y repositorio en memoria (búsqueda, radio, reseñas y datos de ejemplo).
- `dotnet build` sin advertencias ni errores.
- Migración EF Core con PostGIS incluida en el repositorio.
- Arranque verificado sin PostgreSQL (modo demo) y con la API respondiendo a listado, detalle, filtro por radio y creación de reseñas.

#### Frontend

```bash
cd frontend
npm run lint
npm run build
```

- TypeScript en modo estricto y build de producción correcto.
- Bundle dividido: app principal ≈ 339 kB (104 kB gzip) y mapa ≈ 190 kB (54 kB gzip) cargado con `React.lazy`.
- CSS ≈ 42 kB (12 kB gzip).

### Auditoría Lighthouse (build de producción)

| Categoría | Puntuación |
| --- | --- |
| Accessibility | **100** |
| Best Practices | **100** |
| SEO | **100** |

Comprobaciones manuales realizadas:

- Responsive a 390 px, 768 px, 1024 px y 1440 px sin desbordamientos horizontales.
- Navegación por teclado: skip link, foco visible, tabs con flechas, formulario con foco en el primer error.
- Contraste de texto corregido (AA) y atributos ARIA válidos.
- Estados de carga, error, vacío y reintento probados.
- Búsqueda con acentos (`malasaña` → `Malasaña`), filtros combinados, favoritos y publicación de reseñas.
- Flujo de geolocalización completo: petición automática al cargar, concesión (orden por distancia + punto en el mapa), denegación (mensaje, cafeterías de referencia y reintento), apagado (vista original) y selección de ficha (zoom a la cafetería).
- Favoritos independientes de la ubicación: se conservan al cambiar de zona o de ciudad.
- Cabeceras de seguridad en API (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, CSP) y CSP en el HTML estático.
- Rate limiting en reseñas y validación de parámetros de búsqueda.

### Despliegue en GitHub Pages

El workflow `.github/workflows/deploy-pages.yml` compila el frontend y publica `frontend/dist` en Pages en cada push a `main`.

1. Sube el repositorio a GitHub.
2. En **Settings → Pages**, selecciona **GitHub Actions** como origen.
3. El workflow calcula `VITE_SITE_URL` a partir del nombre del repositorio.
4. Opcional: define el variable `SITE_URL` en **Settings → Secrets and variables → Actions** si usas un dominio propio.

Con `base: './'` los assets funcionan tanto en la raíz como en una subruta de proyecto (`usuario.github.io/repo/`).

El workflow `.github/workflows/backend.yml` compila y ejecuta las pruebas del backend en cada push o pull request que afecte a `backend/`.

### Limitaciones conocidas

- La demo de GitHub Pages funciona en **modo demo**: no hay backend público, por lo que las reseñas se guardan solo en el navegador. Los cafés provienen de APIs públicas de OpenStreetMap (Photon/Overpass), sujetas a disponibilidad.
- El backend se ha validado con el repositorio en memoria (modo demo); contra PostgreSQL real requiere Docker/PostgreSQL locales, pero el `docker-compose.yml` y la migración están listos para probarlo de extremo a extremo.
- El mapa depende de OpenStreetMap; sin conexión, los tiles no cargan (el resto de la interfaz sigue funcionando).
- `VITE_SITE_URL` debe sustituirse por la URL real antes de publicar.
- La geolocalización requiere HTTPS (o `localhost`) y el permiso del usuario. Algunos navegadores bloquean los avisos de permiso sin un gesto del usuario (Chrome aplica «quiet blocks»): en ese caso la app muestra el aviso con el botón «Permitir ubicación». Si el permiso está bloqueado de forma permanente, hay que abrirlo desde el candado de la barra de direcciones.

### Licencia

Proyecto de portafolio. Los datos de cafeterías son ficticios y se usan solo como demostración.