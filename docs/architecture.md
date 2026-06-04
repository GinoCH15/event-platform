# 🏗️ Arquitectura — Plataforma de Eventos Online

## Visión General

Sistema distribuido basado en **microservicios con arquitectura orientada a eventos**, diseñado para soportar alta concurrencia, consistencia eventual y disponibilidad parcial.

---

## Diagrama de Arquitectura (Alto Nivel)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          CLIENTES / ACTORES                              │
│   [Cliente Final]  [Organizador]  [Admin]  [Staff Puerta]               │
└────────────┬───────────────────────────────────────────────┬────────────┘
             │ HTTPS                                          │ HTTPS
             ▼                                               ▼
┌─────────────────────────┐                    ┌────────────────────────┐
│      API GATEWAY        │                    │    STAFF APP (PWA)     │
│  (Kong / AWS API GW)    │                    │    Check-in Offline    │
│  - Rate Limiting        │                    └────────────────────────┘
│  - JWT Validation       │
│  - Routing              │
└───────────┬─────────────┘
            │
  ┌─────────┴──────────────────────────────────────────────────┐
  │                    MICROSERVICIOS (ECS Fargate / Docker)    │
  │                                                             │
  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
  │  │ EventService │  │TicketService │  │  PaymentService  │  │
  │  │  .NET 9      │  │  .NET 9      │  │    .NET 9        │  │
  │  │  PostgreSQL  │  │  PostgreSQL  │  │  PostgreSQL      │  │
  │  │  Redis Cache │  │  Redis Lock  │  │  + Outbox        │  │
  │  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
  │         │                 │                    │             │
  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
  │  │  UserService │  │SearchService │  │NotificationSvc   │  │
  │  │  .NET 9      │  │  .NET 9      │  │   .NET 9         │  │
  │  │  PostgreSQL  │  │ Elasticsearch│  │   MongoDB        │  │
  │  │  Keycloak    │  │  + Redis     │  │   RabbitMQ       │  │
  │  └──────────────┘  └──────────────┘  └──────────────────┘  │
  └─────────────────────────┬───────────────────────────────────┘
                            │
            ┌───────────────▼───────────────┐
            │     MESSAGE BROKER            │
            │     RabbitMQ / AWS SQS+SNS    │
            │  Topics: events, tickets,     │
            │  payments, notifications      │
            └───────────────────────────────┘
```

---

## Microservicios

| Servicio            | Responsabilidad                                   | BD SQL       | BD NoSQL      | Cache  |
|---------------------|---------------------------------------------------|--------------|---------------|--------|
| **EventService**    | CRUD eventos, zonas, publicación                  | PostgreSQL   | —             | Redis  |
| **TicketService**   | Reserva/venta tickets, control de aforo           | PostgreSQL   | —             | Redis  |
| **PaymentService**  | Integración PSP, reembolsos, Outbox pattern       | PostgreSQL   | —             | —      |
| **UserService**     | Usuarios, roles, autenticación (OIDC/OAuth2)      | PostgreSQL   | —             | Redis  |
| **SearchService**   | Búsqueda avanzada de eventos                      | —            | Elasticsearch | Redis  |
| **NotificationSvc** | Email/SMS/Push/WhatsApp, idempotencia             | —            | MongoDB       | —      |
| **CheckInService**  | Validación QR, modo offline                       | SQLite local | —             | Redis  |

---

## Flujos Principales

### Flujo Síncrono — Crear Evento
```
Admin → API GW → EventService (POST /events)
  → Valida JWT + Roles
  → Crea Event + Zones en PostgreSQL (transacción)
  → Publica EventCreated → RabbitMQ
  → Responde 201 Created
```

### Flujo Asíncrono — Notificación post-creación
```
RabbitMQ [EventCreated]
  → NotificationService (Consumer)
      → Verifica idempotencia (messageId en MongoDB)
      → Envía email al organizador
      → Registra procesamiento
  → SearchService (Consumer)
      → Indexa evento en Elasticsearch
```

### Flujo Alta Concurrencia — Compra de Tickets
```
Cliente → API GW → TicketService (POST /tickets/reserve)
  → Distributed Lock en Redis (por zona)
  → Verifica disponibilidad en PostgreSQL (SELECT FOR UPDATE)
  → Crea reserva con TTL (10 min)
  → Publica TicketReserved → RabbitMQ
  → PaymentService inicia cobro
  → Si OK: publica TicketConfirmed
  → Si FAIL: publica TicketReleased → libera cupo
```

---

## Seguridad

- **Autenticación**: OAuth 2.0 + OIDC con Keycloak (o AWS Cognito)
- **Tokens**: JWT con RS256, expiración corta (15min) + Refresh Token
- **Roles**: `admin`, `organizer`, `customer`, `staff`
- **API Gateway**: Valida JWT antes de enrutar, rate limiting por IP/usuario
- **TLS**: Todo el tráfico interno y externo cifrado
- **Secrets**: AWS Secrets Manager / HashiCorp Vault

---

## Patrones de Resiliencia

| Patrón               | Implementación          | Dónde aplica                    |
|----------------------|-------------------------|---------------------------------|
| **Circuit Breaker**  | Polly                   | Llamadas a PSP externos         |
| **Retry + Backoff**  | Polly                   | Mensajería, BD                  |
| **Distributed Lock** | Redis (Redlock)         | Reserva de tickets concurrente  |
| **Outbox Pattern**   | EF + Background Worker  | PaymentService → Broker         |
| **Idempotency Key**  | MongoDB (messageId)     | NotificationService             |
| **CQRS**             | MediatR                 | EventService, TicketService     |
| **Saga**             | MassTransit Saga        | Flujo compra completo           |

---

## Infraestructura AWS (Producción)

```
Route53 → CloudFront → ALB
  → ECS Fargate (microservicios)
  → RDS PostgreSQL (Multi-AZ)
  → ElastiCache Redis (Cluster)
  → Amazon MQ (RabbitMQ) / SQS+SNS
  → OpenSearch (Elasticsearch)
  → DocumentDB (MongoDB compatible)
  → S3 (tickets PDF/QR, assets)
  → Cognito (Identity Provider)
  → CloudWatch + X-Ray (Observabilidad)
```

---

## MVP — Scope del Reto

El MVP cubre:
- ✅ **EventService**: API .NET 9, PostgreSQL, Redis, RabbitMQ
- ✅ **NotificationService**: Consumer RabbitMQ, idempotencia MongoDB
- ✅ **Frontend React**: Formulario registro de eventos con JWT
- ✅ **Docker Compose**: Levanta todos los servicios localmente
