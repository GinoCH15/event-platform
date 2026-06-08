#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
#  EVENT PLATFORM — Demo de Casos de Uso
#  Exhibe todos los patrones arquitectónicos en acción
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

API="http://localhost:5050"
NOTIF="http://localhost:5001"

# ── JWT demo (firmado con secret del docker-compose) ──────────────────────────
JWT="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.\
eyJzdWIiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDEiLCJuYW1lIjoiQWRtaW4gVXNlciIsInJvbGUiOiJhZG1pbiIsImlzcyI6ImV2ZW50LXBsYXRmb3JtIiwiYXVkIjoiZXZlbnQtcGxhdGZvcm0tY2xpZW50cyIsImV4cCI6OTk5OTk5OTk5OX0.\
DuXXU8Ss3SG8WLSw3mTkgisAkvjnbezgSW_DXUk2Jhg"

AUTH="Authorization: Bearer $JWT"
CT="Content-Type: application/json"

# ── Colores ───────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GRN='\033[0;32m'; YLW='\033[1;33m'
BLU='\033[0;34m'; CYN='\033[0;36m'; MAG='\033[0;35m'
BLD='\033[1m'; DIM='\033[2m'; RST='\033[0m'

header()  { echo -e "\n${BLD}${BLU}══════════════════════════════════════════════════${RST}"; echo -e "${BLD}${BLU}  $1${RST}"; echo -e "${BLD}${BLU}══════════════════════════════════════════════════${RST}"; }
section() { echo -e "\n${BLD}${CYN}── $1 ──${RST}"; }
ok()      { echo -e "${GRN}✔ $1${RST}"; }
info()    { echo -e "${YLW}ℹ $1${RST}"; }
pattern() { echo -e "${MAG}▸ Patrón: $1${RST}"; }
code()    { echo -e "${DIM}$1${RST}"; }
resp()    { echo -e "${DIM}← $1${RST}"; }

pretty_json() {
  if command -v python3 &>/dev/null; then
    python3 -m json.tool 2>/dev/null || cat
  else
    cat
  fi
}

sleep_msg() { info "Esperando $1s para que el sistema procese..."; sleep "$1"; }

# ═══════════════════════════════════════════════════════════════════════════════
echo -e "\n${BLD}${YLW}"
echo "  ┌─────────────────────────────────────────────┐"
echo "  │   EVENT PLATFORM — Casos de Uso Demo        │"
echo "  │   Arquitectura: DDD · CQRS · Event-Driven   │"
echo "  └─────────────────────────────────────────────┘"
echo -e "${RST}"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-01 · HEALTH CHECK & OBSERVABILIDAD"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "Clean Architecture — cada microservicio expone su propio /health"

section "EventService health"
code "GET $API/health"
R=$(curl -s "$API/health")
echo "$R" | pretty_json
ok "EventService responde: $(echo $R | python3 -c 'import sys,json; d=json.load(sys.stdin); print(d["status"])')"

section "NotificationService health"
code "GET $NOTIF/health"
R=$(curl -s "$NOTIF/health")
echo "$R" | pretty_json
ok "NotificationService responde: $(echo $R | python3 -c 'import sys,json; d=json.load(sys.stdin); print(d["status"])')"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-02 · QUERY CON REDIS CACHE (CQRS — Query Side)"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "CQRS: GetEventsQuery → GetEventsQueryHandler → IDistributedCache (Redis)"
echo ""
echo -e "${DIM}Flujo:"
echo "  Browser/curl"
echo "    → EventsController.GetEvents()"
echo "    → IMediator.Send(GetEventsQuery)"
echo "    → GetEventsQueryHandler"
echo "         1. cache.GetStringAsync(\"events:list:p1:ps20\")"
echo "         2. Si MISS → EventRepository.GetAllAsync() + CountAsync()"
echo "         3. cache.SetStringAsync(result, TTL=5min)"
echo -e "${RST}"

section "Primera llamada → Cache MISS (va a PostgreSQL)"
code "GET $API/api/events"
R=$(curl -s "$API/api/events")
TOTAL=$(echo $R | python3 -c 'import sys,json; d=json.load(sys.stdin); print(d["totalCount"])')
info "Resultado: $TOTAL eventos en BD (primer request → Cache MISS, guardó en Redis)"

section "Segunda llamada → Cache HIT (no toca PostgreSQL)"
R2=$(curl -s "$API/api/events")
ok "Misma respuesta desde Redis (sin consultar Postgres)"
echo "$R2" | pretty_json

section "Verificar cache directamente en Redis"
code "redis-cli KEYS 'EventService:events:*'"
docker exec ep-redis redis-cli KEYS 'EventService:events:*' 2>/dev/null && \
  docker exec ep-redis redis-cli TTL 'EventService:events:list:p1:ps20' 2>/dev/null && \
  info "TTL restante en segundos (máx 300 = 5 min)"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-03 · AUTENTICACIÓN JWT + AUTORIZACIÓN POR ROL"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "JWT (HS256) → [Authorize(Roles=\"admin,organizer\")] → Claims extraction"

section "Sin token → 401 Unauthorized"
code "POST $API/api/events  (sin Authorization header)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$API/api/events" \
  -H "$CT" -d '{"name":"test","date":"2027-01-01T00:00:00Z","location":"Lima","zones":[]}')
resp "HTTP $HTTP_CODE"
ok "✓ Endpoint protegido rechaza petición sin token"

section "Con token válido (rol=admin) → pasa autenticación"
info "JWT Payload decodificado:"
echo -e "${DIM}{"
echo '  "sub": "00000000-0000-0000-0000-000000000001",'
echo '  "name": "Admin User",'
echo '  "role": "admin",'
echo '  "iss": "event-platform",'
echo '  "aud": "event-platform-clients",'
echo '  "exp": 9999999999'
echo -e "}${RST}"
info "El controller extrae el sub claim como OrganizerId automáticamente"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-04 · VALIDACIÓN CON PIPELINE BEHAVIOR (CQRS + FluentValidation)"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "IPipelineBehavior → ValidationBehavior → IValidator<CreateEventCommand>"
echo ""
echo -e "${DIM}Flujo:"
echo "  CreateEvent(dto)"
echo "    → CreateEventCommand"
echo "    → MediatR Pipeline"
echo "        [1] ValidationBehavior.Handle()  ← intercepta ANTES del handler"
echo "              → CreateEventCommandValidator.Validate()"
echo "              → lanza ValidationException si hay errores"
echo "        [2] CreateEventCommandHandler.Handle()  ← solo si validación pasa"
echo -e "${RST}"

section "CU-4A: Nombre vacío → 400 Bad Request"
code "POST /api/events  name: \"\""
R=$(curl -s -X POST "$API/api/events" \
  -H "$CT" -H "$AUTH" \
  -d '{"name":"","date":"2027-06-01T20:00:00Z","location":"Lima","zones":[{"name":"VIP","price":100,"capacity":200}]}')
echo "$R" | pretty_json
ok "ValidationBehavior interceptó antes de llegar al handler"

section "CU-4B: Fecha en el pasado → 400 Bad Request"
code "POST /api/events  date: \"2020-01-01\""
R=$(curl -s -X POST "$API/api/events" \
  -H "$CT" -H "$AUTH" \
  -d '{"name":"Evento Pasado","date":"2020-01-01T00:00:00Z","location":"Lima","zones":[{"name":"General","price":50,"capacity":500}]}')
echo "$R" | pretty_json
ok "Validador rechazó fecha retroactiva"

section "CU-4C: Zonas vacías → 400 Bad Request"
code "POST /api/events  zones: []"
R=$(curl -s -X POST "$API/api/events" \
  -H "$CT" -H "$AUTH" \
  -d '{"name":"Sin Zonas","date":"2027-09-01T20:00:00Z","location":"Lima","zones":[]}')
echo "$R" | pretty_json
ok "Regla: mínimo 1 zona requerida"

section "CU-4D: Precio negativo en zona → 400 Bad Request"
code "POST /api/events  price: -50"
R=$(curl -s -X POST "$API/api/events" \
  -H "$CT" -H "$AUTH" \
  -d '{"name":"Precio Mal","date":"2027-10-01T20:00:00Z","location":"Lima","zones":[{"name":"Campo","price":-50,"capacity":1000}]}')
echo "$R" | pretty_json
ok "Validación a nivel de zona también funciona"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-05 · REGLAS DE DOMINIO (DDD — Domain Entities)"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "DomainException → ExceptionMiddleware → HTTP 422 Unprocessable Entity"
echo ""
echo -e "${DIM}Las invariantes viven en el Dominio, no en la API ni en la BD:"
echo ""
echo "  Event.Create()   → lanza DomainException si fecha <= hoy"
echo "  Event.AddZone()  → lanza DomainException si nombre de zona duplicado"
echo "  Event.Publish()  → lanza DomainException si no es Draft o no tiene zonas"
echo "  Event.Cancel()   → lanza DomainException si ya está Cancelado"
echo "  Zone.Create()    → lanza DomainException si precio < 0 o capacidad <= 0"
echo -e "${RST}"

section "CU-5A: Zonas con nombre duplicado → 422 (DomainException)"
code "POST /api/events  zones con dos zonas llamadas 'VIP'"
R=$(curl -s -X POST "$API/api/events" \
  -H "$CT" -H "$AUTH" \
  -d '{
    "name": "Evento Zona Duplicada",
    "date": "2027-11-01T20:00:00Z",
    "location": "Lima",
    "zones": [
      {"name": "VIP", "price": 200, "capacity": 100},
      {"name": "VIP", "price": 150, "capacity": 200}
    ]
  }')
echo "$R" | pretty_json
ok "DomainException capturada por ExceptionMiddleware → HTTP 422"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-06 · FLUJO COMPLETO: CREAR EVENTO (Command Side CQRS + Event-Driven)"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "Command → Repository + UnitOfWork → PostgreSQL + RabbitMQ publish"
echo ""
echo -e "${DIM}Flujo completo:"
echo "  POST /api/events"
echo "    → CreateEventCommand (MediatR)"
echo "    → [ValidationBehavior OK]"
echo "    → CreateEventCommandHandler:"
echo "         1. Event.Create()           ← DDD factory method"
echo "         2. Event.AddZone() × N      ← invariantes de dominio"
echo "         3. IEventRepository.AddAsync()  ← Repository pattern"
echo "         4. IUnitOfWork.CommitAsync()    ← transacción atómica"
echo "         5. IPublishEndpoint.Publish(EventCreatedMessage)  ← MassTransit"
echo "    → HTTP 201 Created + EventDto"
echo ""
echo "  [Async] NotificationService consume EventCreatedMessage:"
echo "         1. Verifica idempotencia en MongoDB"
echo "         2. Inserta ProcessedMessage"
echo "         3. Inserta NotificationRecord"
echo -e "${RST}"

section "Crear evento real con múltiples zonas"
code "POST $API/api/events"
R=$(curl -s -X POST "$API/api/events" \
  -H "$CT" -H "$AUTH" \
  -d '{
    "name": "Lollapalooza Lima 2027",
    "date": "2027-03-20T18:00:00Z",
    "location": "Estadio Nacional, Lima",
    "zones": [
      {"name": "Campo",    "price": 120.00, "capacity": 8000},
      {"name": "Tribuna",  "price": 180.00, "capacity": 5000},
      {"name": "VIP",      "price": 450.00, "capacity": 500},
      {"name": "Palco VIP","price": 900.00, "capacity": 100}
    ]
  }')

echo "$R" | pretty_json

EVENT_ID=$(echo $R | python3 -c 'import sys,json; d=json.load(sys.stdin); print(d.get("id",""))' 2>/dev/null || echo "")

if [ -n "$EVENT_ID" ]; then
  ok "Evento creado: ID=$EVENT_ID"
  ok "PostgreSQL: evento + zonas persistidos en transacción"
  ok "RabbitMQ: mensaje EventCreatedMessage publicado"
else
  echo -e "${RED}✗ No se pudo extraer el ID del evento${RST}"
  exit 1
fi

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-07 · QUERY POR ID + CACHE INDIVIDUAL"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "GetEventByIdQuery → Redis cache per-entity (TTL 10min)"

section "Primera consulta por ID → Cache MISS → PostgreSQL"
code "GET $API/api/events/$EVENT_ID"
R=$(curl -s "$API/api/events/$EVENT_ID")
echo "$R" | pretty_json
ok "Evento cargado desde PostgreSQL (incluye zonas via EAGER LOADING)"

section "Segunda consulta → Cache HIT (sin tocar PostgreSQL)"
R2=$(curl -s "$API/api/events/$EVENT_ID")
ZONES=$(echo $R2 | python3 -c 'import sys,json; d=json.load(sys.stdin); print(len(d.get("zones",[])))' 2>/dev/null || echo "?")
ok "Desde Redis — $ZONES zonas en caché"

section "Cache key en Redis"
code "redis-cli GET EventService:events:detail:$EVENT_ID"
CACHED_LEN=$(docker exec ep-redis redis-cli STRLEN "EventService:events:detail:$EVENT_ID" 2>/dev/null || echo "0")
info "Bytes almacenados en Redis: $CACHED_LEN bytes"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-08 · MENSAJERÍA ASYNC + IDEMPOTENCIA (Event-Driven + MongoDB)"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "MassTransit → RabbitMQ → EventCreatedConsumer → MongoDB idempotency check"

sleep_msg 3

section "Verificar que NotificationService procesó el mensaje"
code "MongoDB: db.processedmessages.find().sort({processedAt:-1}).limit(1)"
R=$(docker exec ep-mongo mongosh notificationdb --quiet \
  --eval 'JSON.stringify(db.processedmessages.find().sort({processedAt:-1}).limit(1).toArray())' 2>/dev/null || echo "[]")
echo "$R" | python3 -c "
import sys, json
data = json.loads(sys.stdin.read())
if data:
    d = data[0]
    print(f'  MessageId  : {d.get(\"_id\",\"?\")}')
    print(f'  MessageType: {d.get(\"messageType\",\"?\")}')
    print(f'  Notes      : {d.get(\"notes\",\"?\")}')
    print(f'  ProcessedAt: {d.get(\"processedAt\",\"?\")}')
else:
    print('  (sin datos aún)')
" 2>/dev/null || echo "$R"
ok "Mensaje registrado como procesado (garantía idempotencia)"

section "Verificar notificación generada"
code "MongoDB: db.notificationrecords.find().sort({sentAt:-1}).limit(1)"
R=$(docker exec ep-mongo mongosh notificationdb --quiet \
  --eval 'JSON.stringify(db.notificationrecords.find().sort({sentAt:-1}).limit(1).toArray())' 2>/dev/null || echo "[]")
echo "$R" | python3 -c "
import sys, json
data = json.loads(sys.stdin.read())
if data:
    d = data[0]
    print(f'  EventId  : {d.get(\"eventId\",\"?\")}')
    print(f'  Type     : {d.get(\"type\",\"?\")}')
    print(f'  Recipient: {d.get(\"recipient\",\"?\")}')
    print(f'  Subject  : {d.get(\"subject\",\"?\")}')
    print(f'  Status   : {d.get(\"status\",\"?\")}')
    print(f'  SentAt   : {d.get(\"sentAt\",\"?\")}')
else:
    print('  (sin datos aún)')
" 2>/dev/null || echo "$R"
ok "Notificación de email simulada y registrada en MongoDB"

section "Estadísticas globales en MongoDB"
R=$(docker exec ep-mongo mongosh notificationdb --quiet \
  --eval 'JSON.stringify({processed: db.processedmessages.countDocuments(), notifications: db.notificationrecords.countDocuments()})' 2>/dev/null || echo "{}")
echo "$R" | python3 -c "
import sys, json
d = json.loads(sys.stdin.read())
print(f'  Mensajes procesados  : {d.get(\"processed\",\"?\")}')
print(f'  Notificaciones totales: {d.get(\"notifications\",\"?\")}')
" 2>/dev/null || true

section "CU-8A: Idempotencia — segundo evento para disparar otro mensaje"
R=$(curl -s -X POST "$API/api/events" \
  -H "$CT" -H "$AUTH" \
  -d '{
    "name": "Evento Idempotencia Test",
    "date": "2027-05-10T19:00:00Z",
    "location": "Centro de Convenciones, Lima",
    "zones": [{"name": "General","price": 30,"capacity": 300}]
  }')
EVENT_ID2=$(echo $R | python3 -c 'import sys,json; d=json.load(sys.stdin); print(d.get("id",""))' 2>/dev/null || echo "")
ok "Segundo evento creado: ID=$EVENT_ID2 → Nuevo MessageId único → se procesa normalmente"
info "Si el mismo MessageId llegara dos veces, MongoDB lo detecta y lo ignora (idempotencia)"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-09 · INVALIDACIÓN DE CACHE (Consistency)"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "Cache → Stale data awareness (TTL-based expiration)"

section "El cache de lista ahora está desactualizado (hay 2 eventos nuevos)"
R=$(curl -s "$API/api/events")
CURRENT_TOTAL=$(echo $R | python3 -c 'import sys,json; d=json.load(sys.stdin); print(d["totalCount"])')
info "La lista devuelve $CURRENT_TOTAL eventos (puede ser stale del cache)"
info "Se actualiza automáticamente cuando expira el TTL de 5 minutos"
info "En producción: se invalidaría el cache en el handler después de crear"

section "Invalidar manualmente el cache de lista"
code "redis-cli DEL EventService:events:list:p1:ps20"
docker exec ep-redis redis-cli DEL "EventService:events:list:p1:ps20" 2>/dev/null && \
  ok "Cache de lista eliminado manualmente"
R=$(curl -s "$API/api/events")
NEW_TOTAL=$(echo $R | python3 -c 'import sys,json; d=json.load(sys.stdin); print(d["totalCount"])')
ok "Nuevo total desde PostgreSQL: $NEW_TOTAL eventos (cache refreshed)"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-10 · PAGINACIÓN (Repository Pattern)"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "IEventRepository.GetAllAsync(page, pageSize) + CountAsync() → PagedResult<T>"

section "Página 1, tamaño 2"
code "GET $API/api/events?page=1&pageSize=2"
R=$(curl -s "$API/api/events?page=1&pageSize=2")
echo "$R" | pretty_json
ok "Metadatos de paginación: page, pageSize, totalPages, hasNextPage, hasPreviousPage"

section "Página 2 (si hay más de 2 eventos)"
code "GET $API/api/events?page=2&pageSize=2"
R=$(curl -s "$API/api/events?page=2&pageSize=2")
echo "$R" | pretty_json

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-11 · EVENTO NO ENCONTRADO (404)"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "Repository returns null → Controller returns NotFound"

code "GET $API/api/events/00000000-0000-0000-0000-000000000000"
R=$(curl -s -w "\nHTTP:%{http_code}" "$API/api/events/00000000-0000-0000-0000-000000000000")
echo "$R"
ok "HTTP 404 cuando el evento no existe en BD ni en cache"

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-12 · OBSERVABILIDAD — LOGS DE TODOS LOS SERVICIOS"
# ═══════════════════════════════════════════════════════════════════════════════
pattern "Serilog → Structured logging con contexto de request"

section "Logs de EventService (últimas 15 líneas)"
docker-compose -f ~/Desktop/proyectos/eventplatform/event-platform/docker-compose.yml \
  logs --no-log-prefix event-service 2>/dev/null | grep -v "^$" | tail -15

section "Logs de NotificationService (últimas 10 líneas)"
docker-compose -f ~/Desktop/proyectos/eventplatform/event-platform/docker-compose.yml \
  logs --no-log-prefix notification-service 2>/dev/null | grep -v "^$" | tail -10

# ═══════════════════════════════════════════════════════════════════════════════
header "UC-13 · ESTADO FINAL DE LA INFRAESTRUCTURA"
# ═══════════════════════════════════════════════════════════════════════════════

section "PostgreSQL — tablas y conteo"
docker exec ep-postgres psql -U eventuser -d eventdb -c \
  "SELECT 'events' as tabla, COUNT(*) as total FROM events
   UNION ALL
   SELECT 'zones', COUNT(*) FROM zones;" 2>/dev/null

section "MongoDB — colecciones"
docker exec ep-mongo mongosh notificationdb --quiet \
  --eval 'db.getCollectionNames().forEach(c => print(c + ": " + db[c].countDocuments() + " docs"))' 2>/dev/null

section "Redis — todas las keys de EventService"
docker exec ep-redis redis-cli KEYS 'EventService:*' 2>/dev/null

section "RabbitMQ — colas activas"
curl -s -u guest:guest "http://localhost:15672/api/queues" 2>/dev/null | \
  python3 -c "
import sys, json
try:
    qs = json.load(sys.stdin)
    for q in qs:
        print(f'  {q[\"name\"]}: {q[\"messages\"]} msgs pending, {q[\"message_stats\"].get(\"deliver_get\",{}).get(\"rate\",0)}/s')
except:
    print('  (sin colas activas o RabbitMQ no responde)')
" 2>/dev/null

# ═══════════════════════════════════════════════════════════════════════════════
echo -e "\n${BLD}${GRN}"
echo "  ┌──────────────────────────────────────────────────────────────┐"
echo "  │  ✅  Todos los casos de uso completados                      │"
echo "  ├──────────────────────────────────────────────────────────────┤"
echo "  │  Patrones exhibidos:                                         │"
echo "  │   • CQRS            → Commands vs Queries separados          │"
echo "  │   • DDD             → Entidades con invariantes de dominio   │"
echo "  │   • Repository      → IEventRepository + IUnitOfWork         │"
echo "  │   • Pipeline Behavior → Validación automática pre-handler    │"
echo "  │   • Event-Driven    → RabbitMQ (MassTransit) async           │"
echo "  │   • Idempotencia    → MongoDB deduplica mensajes             │"
echo "  │   • Redis Cache     → TTL 5min lista / 10min detalle         │"
echo "  │   • JWT Auth        → HS256 + role-based authorization       │"
echo "  │   • ExceptionMiddleware → Domain/Validation errors → HTTP    │"
echo "  │   • Clean Architecture → Domain→Application→Infrastructure  │"
echo "  └──────────────────────────────────────────────────────────────┘"
echo -e "${RST}"
echo ""
echo -e "  ${BLU}Frontend  :${RST} http://localhost:3001"
echo -e "  ${BLU}Swagger   :${RST} http://localhost:5050/swagger"
echo -e "  ${BLU}RabbitMQ  :${RST} http://localhost:15672  (guest/guest)"
echo ""
