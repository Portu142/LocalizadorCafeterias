# ☕ Localizador de Cafés

Buscador de cafeterías de especialidad cerca de ti. Proyecto **full-stack** pensado como pieza de portafolio: geolocalización automática para ordenar por cercanía, mapa interactivo, filtros por barrio, fichas con menú y horarios, favoritos y reseñas.
**Demo:** _pendiente de publicar en GitHub Pages_ · **API:** ASP.NET Core 8 + PostgreSQL/PostGIS (en desarrollo local)

![Vista de escritorio del Localizador de Cafés](docs/screenshots/desktop.png)

## Cerca de mí

Al cargar la página se solicita permiso de ubicación automáticamente. Si lo concedes, la app busca cafeterías en un radio de 5 km, las ordena de menor a mayor distancia y **centra el mapa en tu posición** (zoom de calle, con tu punto en el centro). Con la API activa se consultan primero las cafeterías de PostgreSQL; si no hay ninguna cerca (la base solo cubre Madrid), se muestran las de OpenStreetMap de tu zona. Si te alejas arrastrando, el botón **«Centrar en mí»** te devuelve a tu ubicación. Si deniegas el permiso (o el navegador bloquea la petición automática), aparece un aviso con el botón «Permitir ubicación»: al concederlo, la app se recentra sola en tu zona sin recargar. Mientras tanto, enseña cafeterías de Madrid como referencia.

Los **favoritos no dependen de la ubicación**: se guarda la ficha del café en `localStorage`, así que siguen apareciendo aunque te muevas a otra ciudad o cambies de zona.

![Localizador de Cafés con la ubicación activa](docs/screenshots/location.png)

---

## Sobre el proyecto

El objetivo era construir algo más que una landing: una aplicación real con separación de capas, consultas geoespaciales, validación de datos, estados de carga y error, accesibilidad y un despliegue reproducible.

- **Frontend** React 19 + TypeScript + Vite + Tailwind CSS, con React Query para el estado del servidor.
- **Backend** ASP.NET Core 8 con arquitectura por capas (`Domain`, `Application`, `Infrastructure`, `Api`).
- **Base de datos** PostgreSQL 16 + PostGIS, con columna `geography(Point)` e índice GIST para búsquedas por cercanía.
- **Demo estática**: si `VITE_API_URL` está vacío, el frontend funciona sin backend y consulta **cafeterías reales de OpenStreetMap** (Photon y Overpass) cerca de tu ubicación. OSM no publica cartas, así que el menú aparece vacío; las reseñas y los favoritos se guardan en `localStorage`.

### Funcionalidades

- **Cerca de mí**: geolocalización del dispositivo con haversine, radio de 5 km y orden por distancia; estados de permiso concedido, denegado, no disponible y timeout. La ubicación se pide automáticamente al cargar.
- Búsqueda por nombre, dirección o barrio, insensible a mayúsculas y acentos.
- Filtro por barrio y vista de favoritos persistentes e independientes de la ubicación.
- **Filtros combinables**: «Abierto ahora» (interpreta el horario de OpenStreetMap o el de la API) y servicios como wifi, terraza, enchufes, accesible, para llevar, mascotas u opciones veganas.
- **Servicios y fotos** en la ficha: etiquetas con icono y fotos con autor y licencia (Wikimedia Commons o subidas por la comunidad).
- **Carta**: si no hay menú publicado, enlace directo a la carta o a la web del local (OpenStreetMap) o a su ficha en Google Maps, donde está la pestaña «Carta».
- **Carta de la comunidad** (con la API): cualquier usuario con sesión puede añadir platos y precios a cualquier café, también a los de OpenStreetMap, que se guardan en la base de datos al recibir su primer plato. Con la API, la lista mezcla los cafés guardados y los de OpenStreetMap sin duplicados.
- **Cuentas** (con la API): registro e inicio de sesión con ASP.NET Identity y cookie HttpOnly; las reseñas se firman con el nombre de la cuenta, se pueden subir fotos y hay una vista «Mis reseñas».
- Mapa Leaflet/OpenStreetMap con marcadores sincronizados con el listado, punto azul con tu posición y encuadre automático de los resultados.
- Ficha de cafetería: distancia a tu posición, horario, teléfono, enlace a Google Maps, menú y reseñas.
- Formulario de reseñas con validación en cliente y servidor, contador de caracteres y estados de éxito/error.
- **Puntuación por plato**: cada reseña puede puntuar de 1 a 5 los platos del menú que haya probado; la ficha muestra la media de cada plato y la media global del menú. Los cafés sin carta publicada muestran un estado vacío claro.
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

#### Todo de una vez (Windows)

Desde la raíz del repositorio:

```powershell
.\scripts\dev.ps1
```

Arranca PostgreSQL (WSL), abre una ventana con la API y otra con el frontend, y abre `http://localhost:5173`. Opciones: `-SkipDatabase` (si la base ya está en marcha) y `-SkipBrowser`.

Si la política de ejecución bloquea el script: `powershell -ExecutionPolicy Bypass -File .\scripts\dev.ps1`.

#### Arranque manual paso a paso (Windows PowerShell)

```powershell
# 1) PostgreSQL en WSL (una vez por sesión)
.\scripts\start-db.ps1

# 2) API .NET + PostgreSQL (terminal 1, desde la raíz)
.\.dotnet\dotnet.exe run --project backend\src\LocalizadorCafes.Api

# 3) Frontend con API (terminal 2)
cd frontend
npm ci
# Crea .env.development.local solo si no existe:
"VITE_API_URL=http://localhost:5067" | Set-Content .env.development.local
npm run dev
```

Abre `http://localhost:5173` (API), `http://localhost:5067/swagger` y `http://localhost:5067/health`.

Hay dos builds de producción:

**Demo estática (GitHub Pages):** cafés reales de OpenStreetMap, sin API y sin carta.

```powershell
cd frontend
npm run build
npm run preview
```

Abre `http://localhost:4173`. El archivo `.env.development.local` no afecta al build: solo se carga con `npm run dev`.

**Túnel con API (menús reales de PostgreSQL):** compila con `VITE_API_URL=same-origin`, de modo que la API se sirve en el mismo origen a través del proxy de `vite preview`.

```powershell
# Requiere PostgreSQL + API arrancados (pasos 1 y 2 de arriba)
cd frontend
npm run build:tunnel
npm run preview -- --port 4173

# En otra terminal, para compartirlo:
& "C:\Program Files (x86)\cloudflared\cloudflared.exe" tunnel --url http://localhost:4173 --no-autoupdate
```

El proxy de `vite preview` reenvía `/api` y `/health` a `http://localhost:5067` y acepta el host `*.trycloudflare.com`. La URL del túnel cambia cada vez que se reinicia.

#### Modo demo (sin backend)

La demo estática **no llama a la API**: React Query consulta **cafeterías reales de OpenStreetMap** (Photon y, como respaldo, Overpass) y las reseñas se guardan en `localStorage`.

- Los cafés, direcciones, horarios y teléfonos son datos reales de OSM.
- **OSM no publica cartas**: el menú aparece vacío. En la ficha hay enlaces a OpenStreetMap y Google Maps para consultar la información del local.
- Para ver menús y puntuación por plato hay que arrancar la API .NET + PostgreSQL (modo completo).

#### 1. Frontend en modo demo (sin backend)

```bash
cd frontend
npm ci
npm run dev
```

Abre `http://localhost:5173`. Sin `VITE_API_URL`, la app consulta cafeterías reales de OpenStreetMap cerca de tu ubicación. Las reseñas y los favoritos se guardan en `localStorage`.

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

Si no tienes el SDK de .NET en el `PATH`, usa el SDK local desde la raíz: `.\.dotnet\dotnet.exe run --project backend\src\LocalizadorCafes.Api`.

- API: `http://localhost:5067`
- Swagger (solo Development): `http://localhost:5067/swagger`
- Health check: `http://localhost:5067/health`

> Para recompilar con la API en marcha hay que pararla antes (`Ctrl+C`): Windows bloquea los `.dll` del proceso y la compilación falla con `MSB3027`. Si arrancas otra instancia, primero libera el puerto con `npx kill-port 5067`.

Si no hay PostgreSQL disponible, la API **arranca en modo demo** con las 6 cafeterías de ejemplo en memoria: todos los endpoints funcionan y las reseñas nuevas se pierden al reiniciar. El aviso aparece en la consola al iniciar.

##### Con PostgreSQL + PostGIS

La forma más rápida en Windows es usar la distro Ubuntu de WSL, que ya trae PostgreSQL 16 y PostGIS:

```powershell
# Instalación inicial (una sola vez)
wsl -d Ubuntu-24.04 -u root -- bash -lc "apt-get install -y postgresql postgresql-contrib postgis postgresql-16-postgis-3"

# Arrancar el clúster (mantiene la distro viva mientras el equipo esté encendido)
.\scripts\start-db.ps1
```

Si usas una distro con otro nombre: `.\scripts\start-db.ps1 -Distro Ubuntu`. El script funciona tanto con Windows PowerShell 5.1 como con PowerShell 7; si la política de ejecución lo bloquea, usa `powershell -ExecutionPolicy Bypass -File .\scripts\start-db.ps1`.

También puedes usar un PostgreSQL nativo o Docker; en cualquier caso crea la base de datos y habilita PostGIS:

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
# frontend/.env.development.local (solo afecta a `npm run dev`)
VITE_API_URL=http://localhost:5067
npm run dev
```

> El build de producción genera la CSP con el origen de `VITE_API_URL` en `connect-src`, así que la API local (HTTP) no queda bloqueada por el navegador.

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
| `VITE_API_URL` | vacío | URL de la API. Vacío = modo demo con cafeterías reales de OpenStreetMap (sin carta). |
| `VITE_SITE_URL` | `https://portu142.github.io/LocalizadorCafeterias/` | URL pública para canonical, Open Graph y sitemap. |

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
| `POST` | `/api/cafes/{id}/reviews` | Añade una reseña. **Requiere sesión**; el autor es el nombre de la cuenta. Limitado a 8 peticiones/minuto por IP. |
| `POST` | `/api/cafes/{id}/photos` | Sube una foto (`multipart/form-data`, campo `photo`; JPEG, PNG o WebP de hasta 5 MB, validada por su contenido). Requiere sesión; 20 por hora. |
| `GET` | `/api/photos/{archivo}` | Sirve una foto subida. |
| `POST` | `/api/cafes/import` | Guarda un café de OpenStreetMap (`{"osmId":"node/123"}`) o devuelve el ya guardado. El servidor descarga sus datos de la API de OSM. Requiere sesión; 30 aportaciones por hora. |
| `POST` | `/api/cafes/{id}/menu` | Añade un plato a la carta (`name`, `price` de 0,01 a 500 €). Requiere sesión. |
| `DELETE` | `/api/cafes/{id}/menu/{platoId}` | Borra un plato propio si nadie más lo ha puntuado. |
| `GET` | `/api/admin/pending` | **Admin.** Platos y fotos pendientes de revisión. |
| `POST` | `/api/admin/dishes/{id}/approve` · `/reject` | **Admin.** Aprueba o rechaza un plato. |
| `POST` | `/api/admin/photos/{id}/approve` · `/reject` | **Admin.** Aprueba o rechaza una foto. |
| `GET` | `/api/admin/reviews` | **Admin.** Últimas reseñas. |
| `DELETE` | `/api/admin/reviews/{id}` | **Admin.** Borra una reseña. |

### Panel de administración

El backend sirve un panel web en **`/admin`** (por ejemplo `http://localhost:5067/admin`, o `/admin` en el túnel), con su propio inicio de sesión:

- **En vivo**: personas usando la web ahora mismo (con y sin cuenta), visitantes por minuto de la última media hora, usuarios conectados y cafeterías que se están viendo; se actualiza sola cada 5 s. La web envía un latido (`POST /api/presence`) cada 30 s con la pestaña visible; los anónimos se identifican con un número aleatorio por sesión del navegador (sessionStorage), no se guardan IPs y todo vive en memoria y caduca en minutos.
- **Resumen**: pendientes de revisar, totales, gráficas semanales (cuentas, reseñas, platos y fotos de las últimas 8 semanas, con tabla de datos) y últimas acciones.
- **Platos** y **Fotos**: pestañas por estado (pendientes, publicados, rechazados, todos), búsqueda y paginación; aprobar, rechazar, ocultar, restaurar, editar nombre y precio, y borrar definitivamente (las fotos subidas se borran también del disco).
- **Reseñas**: búsqueda por texto, autor o cafetería y borrado.
- **Cafeterías**: las guardadas en la base de datos (las de ejemplo y las de OpenStreetMap que han recibido una reseña, foto o plato; con la API, aportar algo a un café de OSM lo guarda automáticamente), con búsqueda y, por cafetería, edición de datos y servicios, su carta, fotos y reseñas, y borrado.
- **Usuarios**: búsqueda, dar o quitar el rol de administrador, **suspender y reactivar** cuentas (una cuenta suspendida no puede iniciar sesión ni publicar y sus sesiones abiertas caducan en menos de un minuto), borrar cuentas (su contenido se conserva sin vincular) y ficha con todo lo que ha aportado.
- **Actividad**: pestaña **Comunidad** con lo que hacen los usuarios (reseñas, platos propuestos, fotos, cafeterías de OpenStreetMap guardadas y cuentas nuevas) y pestaña **Administración** con el registro de acciones del panel (quién, qué y cuándo), filtrable por tipo; ese registro no se edita ni se borra.
- **Contraseña**: cambio de la contraseña propia.

Los platos y las fotos que envían los usuarios quedan **pendientes** (solo los ve su autor, con la etiqueta «Pendiente de revisión») hasta que un administrador los aprueba. Las reseñas se publican al momento.

**Administrador inicial**: si al arrancar no hay ningún administrador, la API crea `admin@localizador.local` con una contraseña aleatoria y guarda las credenciales en `backend/src/LocalizadorCafes.Api/App_Data/admin-inicial.txt` (excluido de git; la contraseña nunca se escribe en el log). Entra, cámbiala en «Contraseña» y borra el archivo. El email se cambia con `Admin:InitialEmail` y la creación se desactiva con `Admin:CreateInitialAdmin=false`.

Para hacer administradora otra cuenta: desde «Usuarios» en el panel, o añadiendo su email a `Admin:Emails` sin guardarlo en el repositorio:

```bash
dotnet user-secrets set "Admin:Emails:0" "tu-email@ejemplo.com" --project backend/src/LocalizadorCafes.Api
```

En Docker o producción se usa la variable `Admin__Emails__0`. Seguridad del panel: solo cuentas con rol Admin, formularios con token antifalsificación, CSP estricta sin JavaScript en línea, `X-Frame-Options: DENY`, sin caché y límite de intentos de inicio de sesión.

### Datos de ejemplo

El seed incluye **6 cafeterías** en 4 barrios de Madrid (Huertas, Malasaña, Palacio, Conde Duque), con menús, horarios y **12 reseñas** iniciales. Los mismos datos alimentan el respaldo en memoria de la API cuando PostgreSQL no está disponible.

La base de datos solo cubre Madrid. Para que la app sea útil en cualquier ciudad, el frontend usa una **fuente híbrida**:

- Consulta primero la API con tu ubicación (radio de 5 km).
- Si la API devuelve resultados, se muestran tal cual (PostgreSQL/PostGIS).
- Si devuelve **0 cafeterías** (estás fuera de Madrid), consulta OpenStreetMap (Photon y, como respaldo, Overpass) y muestra los cafés reales de tu zona. La nota sobre el listado lo indica: «Cafés reales de OpenStreetMap cerca de ti (la API solo cubre Madrid)».
- Los cafés de OSM llevan ids `osm-…`: su detalle y sus reseñas se resuelven en el navegador y **nunca** se envían a la API. Las reseñas de OSM se guardan en `localStorage`.

### Calidad y pruebas

#### Backend

```bash
cd backend
dotnet test
```

- 26 pruebas unitarias: validador de reseñas (autor, longitudes, rating, comentario, máximo de platos y puntuaciones por plato) y servicio/repositorio en memoria (medias, platos válidos, búsqueda, radio y datos de ejemplo).
- `dotnet build` sin advertencias ni errores.
- Migración EF Core con PostGIS incluida en el repositorio.
- Verificado contra PostgreSQL 16 + PostGIS 3.4 reales: migración y seed automáticos, listado, filtro por radio (`geography`), detalle y alta de reseñas con recálculo de la media. También verificado el arranque sin base de datos (modo demo en memoria).

#### Frontend

```bash
cd frontend
npm run lint
npm run build
```

- TypeScript en modo estricto y build de producción correcto.
- Bundle dividido: app principal ≈ 341 kB (104 kB gzip) y mapa ≈ 190 kB (54 kB gzip) cargado con `React.lazy`.
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

- La demo funciona en **modo demo**: no hay backend público, por lo que las reseñas se guardan solo en el navegador y no hay cuentas ni subida de fotos (necesitan la API). Los cafés son reales de OpenStreetMap, que no publica cartas: el menú aparece vacío y no hay puntuación por plato en esa vista. Para los menús hace falta la API .NET + PostgreSQL.
- El dataset de PostgreSQL es **solo de Madrid**: fuera de 5 km la API devuelve `[]` y el frontend cae a OpenStreetMap. Las reseñas de esos cafés de OSM se guardan en `localStorage` (clave `localizador_cafes_mock_reviews`), no en PostgreSQL.
- El backend se ha validado de extremo a extremo contra **PostgreSQL 16 + PostGIS 3.4** reales (migraciones, seed, filtro por radio con `geography`, detalle y alta de reseñas). En Windows puede arrancarse con `scripts/start-db.ps1`; también hay `docker-compose.yml` para Docker.
- El mapa depende de OpenStreetMap; sin conexión, los tiles no cargan (el resto de la interfaz sigue funcionando).
- `VITE_SITE_URL` apunta a `https://portu142.github.io/LocalizadorCafeterias/`; el workflow lo recalcula si cambias de repositorio o usas dominio propio.
- La geolocalización requiere HTTPS (o `localhost`) y el permiso del usuario. Algunos navegadores bloquean los avisos de permiso sin un gesto del usuario (Chrome aplica «quiet blocks»): en ese caso la app muestra el aviso con el botón «Permitir ubicación». Si el permiso está bloqueado de forma permanente, hay que abrirlo desde el candado de la barra de direcciones.

### Licencia

Proyecto de portafolio. Los datos de cafeterías son ficticios y se usan solo como demostración.