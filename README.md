# 🎟️ Event Platform — MVP Técnico

> Plataforma de Eventos Online · Reto Técnico Líder Técnico

Stack: **.NET 9** · **PostgreSQL** · **MongoDB** · **Redis** · **RabbitMQ** · **React 18 + TypeScript**  
Arquitectura: **Microservicios** · **DDD** · **Clean Architecture** · **Event-Driven** · **CQRS**

---

## 📁 Estructura del Proyecto

```
eventplatform/
├── docker-compose.yml          # Orquestación completa local
├── docs/
│   └── architecture.md         # Diagrama y sustentación de arquitectura
├── scripts/
│   └── init-db.sql             # Script SQL de referencia del modelo
└── src/
    ├── EventService/           # API principal (.NET 9)
    │   ├── Api/                # Controllers, Middleware
    │   ├── Application/        # Commands, Queries (CQRS), DTOs
    │   ├── Domain/             # Entidades, Reglas de negocio
    │   └── Infrastructure/     # EF Core, Redis, Repositories
    ├── NotificationService/    # Consumidor de eventos (.NET 9)
    │   ├── Application/        # Consumers (MassTransit)
    │   ├── Contracts/          # Mensajes compartidos
    │   └── Infrastructure/     # MongoDB context
    └── Frontend/               # React 18 + TypeScript
        └── src/
            ├── pages/          # CreateEventPage, EventListPage
            ├── services/       # API client (axios)
            └── types/          # TypeScript types
```

---

## 🚀 Levantar el Proyecto (Docker)

### Prerrequisitos
- Docker Desktop 4.x+ o Podman Desktop
- Git

### 1. Clonar el repositorio
```bash
git clone <repo-url>
cd eventplatform
```

### 2. Levantar todos los servicios
```bash
docker-compose up --build -d
```

Esto levanta:
| Servicio             | Puerto | URL                          |
|----------------------|--------|------------------------------|
| **Frontend**         | 3000   | http://localhost:3000        |
| **EventService API** | 5000   | http://localhost:5000        |
| **Swagger**          | 5000   | http://localhost:5000/swagger|
| **NotificationSvc**  | 5001   | http://localhost:5001/health |
| **RabbitMQ UI**      | 15672  | http://localhost:15672       |
| **PostgreSQL**       | 5432   | localhost:5432               |
| **MongoDB**          | 27017  | localhost:27017              |
| **Redis**            | 6379   | localhost:6379               |

> Credenciales RabbitMQ: `guest` / `guest`

### 3. Verificar que todo esté corriendo
```bash
docker-compose ps
docker-compose logs event-service --follow
```

---

## 🛠️ Desarrollo Local (sin Docker)

### EventService

```bash
cd src/EventService

# Instalar dependencias
dotnet restore

# Configurar connection strings en appsettings.Development.json
# (ajustar localhost en lugar de nombres de contenedor)

# Ejecutar migraciones EF Core
dotnet ef database update

# Iniciar API
dotnet run
```

### Migraciones EF Core

```bash
cd src/EventService

# Crear nueva migración
dotnet ef migrations add NombreMigracion

# Aplicar migraciones a la BD
dotnet ef database update

# Revertir última migración
dotnet ef migrations remove
```

**El proyecto aplica migraciones y seed automáticamente** al arrancar en Development mode (ver `Program.cs` → `DbSeeder.SeedAsync`).

### NotificationService

```bash
cd src/NotificationService
dotnet restore
dotnet run
```

### Frontend

```bash
cd src/Frontend
npm install

# Desarrollo (proxy automático a localhost:5000)
npm run dev

# Build producción
npm run build
```

---

## 🔑 Autenticación JWT

La API usa JWT (HS256). Para el demo, se puede usar el token fijo incluido en el frontend.

**Para generar un token válido**, usa cualquier herramienta JWT con:
```json
{
  "sub": "00000000-0000-0000-0000-000000000001",
  "name": "Admin User",
  "role": "admin",
  "iss": "event-platform",
  "aud": "event-platform-clients",
  "exp": 9999999999
}
```
Secret: `super-secret-key-for-demo-at-least-32-chars!`

En **Swagger**: haz click en **Authorize** → `Bearer {token}`

---

## 📡 API Endpoints

### EventService (`http://localhost:5000`)

| Método | Endpoint              | Auth         | Descripción                          |
|--------|-----------------------|--------------|--------------------------------------|
| `GET`  | `/api/events`         | ❌ Público   | Lista eventos paginados (con cache)  |
| `GET`  | `/api/events/{id}`    | ❌ Público   | Detalle de un evento                 |
| `POST` | `/api/events`         | ✅ JWT       | Crear evento + zonas + publicar msg  |
| `GET`  | `/health`             | ❌ Público   | Health check                         |
| `GET`  | `/swagger`            | ❌ Público   | Documentación interactiva            |

### Ejemplo: Crear Evento

```bash
curl -X POST http://localhost:5000/api/events \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Festival Rock 2026",
    "date": "2026-12-15T20:00:00Z",
    "location": "Estadio Nacional, Lima",
    "zones": [
      { "name": "Campo", "price": 50.00, "capacity": 5000 },
      { "name": "VIP", "price": 200.00, "capacity": 300 }
    ]
  }'
```

---

## 🏗️ Arquitectura

Ver [`docs/architecture.md`](docs/architecture.md) para el diagrama completo y sustentación.

### Patrones implementados

| Patrón               | Dónde                               |
|----------------------|-------------------------------------|
| **CQRS**             | EventService (MediatR)              |
| **Repository**       | EventRepository + IUnitOfWork       |
| **DDD**              | Entidades con invariantes de dominio|
| **Event-Driven**     | EventCreated → RabbitMQ             |
| **Idempotencia**     | NotificationService (MongoDB)       |
| **Redis Cache**      | GET /events (TTL 5 min)             |
| **Pipeline Behavior**| Validación automática (FluentVal.)  |
| **Clean Architecture**| Domain → Application → Infrastructure|

---

## 🧪 Flujo de Prueba

1. Abre **http://localhost:3000** → pantalla de eventos
2. Haz click en **"Crear Evento"** → completa el formulario
3. Al guardar, el EventService:
   - Persiste en PostgreSQL
   - Publica `EventCreated` en RabbitMQ
4. El NotificationService:
   - Consume el mensaje
   - Verifica idempotencia en MongoDB
   - Registra la notificación
5. Verifica en **RabbitMQ UI** (localhost:15672) los mensajes
6. Verifica en **GET /api/events** que se usa el cache Redis

---

## 📦 Variables de Entorno

| Variable                          | Valor por defecto                                |
|-----------------------------------|--------------------------------------------------|
| `ConnectionStrings__Postgres`     | `Host=postgres;Database=eventdb;...`             |
| `ConnectionStrings__Redis`        | `redis:6379`                                     |
| `ConnectionStrings__MongoDB`      | `mongodb://mongo:27017`                          |
| `RabbitMQ__Host`                  | `rabbitmq`                                       |
| `Jwt__Key`                        | `super-secret-key-for-demo-at-least-32-chars!`   |
| `Jwt__Issuer`                     | `event-platform`                                 |
| `Jwt__Audience`                   | `event-platform-clients`                         |
