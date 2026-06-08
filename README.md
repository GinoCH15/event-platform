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
│   ├── init-db.sql             # Script SQL de referencia del modelo de datos
│   └── demo-casos-de-uso.sh    # Demo interactivo de todos los patrones
└── src/
    ├── EventService/           # API principal (.NET 9)
    │   ├── Api/                # Controllers, Middleware (ExceptionMiddleware)
    │   ├── Application/        # Commands, Queries (CQRS + MediatR), DTOs
    │   ├── Domain/             # Entidades DDD con invariantes, DomainException
    │   └── Infrastructure/     # EF Core + PostgreSQL, Redis, Repositories
    ├── NotificationService/    # Consumidor de eventos (.NET 9)
    │   ├── Application/        # Consumers (MassTransit)
    │   ├── Contracts/          # Mensajes compartidos (EventCreatedMessage)
    │   └── Infrastructure/     # MongoDB context, idempotencia
    └── Frontend/               # React 18 + TypeScript
        └── src/
            ├── pages/          # CreateEventPage, EventListPage
            ├── services/       # API client (axios + JWT)
            └── types/          # TypeScript types
```

---

## 🚀 Levantar el Proyecto (Docker)

### Prerrequisitos
- Docker Desktop 4.x+ con soporte Rosetta (Apple Silicon)
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

Esto levanta automáticamente:
| Servicio             | Puerto | URL                           |
|----------------------|--------|-------------------------------|
| **Frontend**         | 3001   | http://localhost:3001         |
| **EventService API** | 5050   | http://localhost:5050         |
| **Swagger**          | 5050   | http://localhost:5050/swagger |
| **NotificationSvc**  | 5001   | http://localhost:5001/health  |
| **RabbitMQ UI**      | 15672  | http://localhost:15672        |
| **PostgreSQL**       | 5432   | localhost:5432                |
| **MongoDB**          | 27017  | localhost:27017               |
| **Redis**            | 6379   | localhost:6379                |

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

**El proyecto aplica migraciones y seed automáticamente** al arrancar (ver `Program.cs` → `DbSeeder.SeedAsync`).  
El seeder crea eventos de ejemplo en PostgreSQL para que el frontend muestre datos desde el primer arranque.

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

# Desarrollo (proxy automático a EventService en puerto 5050)
npm run dev

# Build producción
npm run build
```

---

## 🔑 Autenticación JWT

La API usa **JWT HS256**. Para el demo, el frontend incluye un token precargado y válido.

**Token de demo** (rol `admin`, exp. lejana):
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDEiLCJuYW1lIjoiQWRtaW4gVXNlciIsInJvbGUiOiJhZG1pbiIsImlzcyI6ImV2ZW50LXBsYXRmb3JtIiwiYXVkIjoiZXZlbnQtcGxhdGZvcm0tY2xpZW50cyIsImV4cCI6OTk5OTk5OTk5OX0.DuXXU8Ss3SG8WLSw3mTkgisAkvjnbezgSW_DXUk2Jhg
```

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
Secret: `super-secret-key-for-demo-at-least-32-chars!` · Algoritmo: `HS256`

En **Swagger**: haz click en **Authorize** → pega `Bearer <token>`

---

## 📡 API Endpoints

### EventService (`http://localhost:5050`)

| Método | Endpoint              | Auth         | Descripción                          |
|--------|-----------------------|--------------|--------------------------------------|
| `GET`  | `/api/events`         | ❌ Público   | Lista eventos paginados (con cache)  |
| `GET`  | `/api/events/{id}`    | ❌ Público   | Detalle de un evento                 |
| `POST` | `/api/events`         | ✅ JWT       | Crear evento + zonas + publicar msg  |
| `GET`  | `/health`             | ❌ Público   | Health check                         |
| `GET`  | `/swagger`            | ❌ Público   | Documentación interactiva            |

### Ejemplo: Crear Evento

```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDEiLCJuYW1lIjoiQWRtaW4gVXNlciIsInJvbGUiOiJhZG1pbiIsImlzcyI6ImV2ZW50LXBsYXRmb3JtIiwiYXVkIjoiZXZlbnQtcGxhdGZvcm0tY2xpZW50cyIsImV4cCI6OTk5OTk5OTk5OX0.DuXXU8Ss3SG8WLSw3mTkgisAkvjnbezgSW_DXUk2Jhg"

curl -X POST http://localhost:5050/api/events \
  -H "Authorization: Bearer $TOKEN" \
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

Ver [`docs/architecture.md`](docs/architecture.md) para el diagrama completo y sustentación técnica.

### Patrones implementados

| Patrón                | Implementación                           | Dónde                              |
|-----------------------|------------------------------------------|------------------------------------|
| **Clean Architecture**| Domain → Application → Infrastructure   | EventService (capas separadas)     |
| **DDD**               | `Event.Create()`, `DomainException`      | `Domain/Entities/Event.cs`         |
| **CQRS**              | `CreateEventCommand`, `GetEventsQuery`   | Application + MediatR              |
| **Pipeline Behavior** | `ValidationBehavior<,>`                  | FluentValidation automático        |
| **Repository + UoW**  | `IEventRepository`, `IUnitOfWork`        | Infrastructure + EF Core           |
| **Event-Driven**      | `EventCreatedMessage` → RabbitMQ         | MassTransit `IPublishEndpoint`     |
| **Idempotencia**      | `ProcessedMessage` keyed by `MessageId`  | NotificationService + MongoDB      |
| **Redis Cache**       | `IDistributedCache`, TTL 5/10 min        | GET /events, GET /events/{id}      |
| **ExceptionMiddleware**| `DomainException`→422, `Validation`→400 | `Api/Middleware/`                  |

---

## 🧪 Flujo de Prueba

1. Abre **http://localhost:3001** → pantalla de eventos
2. Haz click en **"Crear Evento"** → completa el formulario
3. Al guardar, el EventService:
   - Valida con FluentValidation (pipeline behavior)
   - Persiste en PostgreSQL (transacción)
   - Publica `EventCreated` en RabbitMQ
   - Invalida el cache Redis
4. El NotificationService:
   - Consume el mensaje (MassTransit)
   - Verifica idempotencia en MongoDB (`processed_messages`)
   - Registra la notificación en MongoDB (`notifications`)
5. Verifica en **RabbitMQ UI** (http://localhost:15672) los exchanges y colas
6. Verifica en **GET /api/events** (primera llamada: MISS, segunda: HIT en logs)

### Demo automatizado (todos los patrones)

```bash
bash scripts/demo-casos-de-uso.sh
```

Ejecuta 13 casos de uso cubriendo: autenticación, CRUD, validación, cache, mensajería, idempotencia y errores.

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
