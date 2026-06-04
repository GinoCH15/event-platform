.PHONY: up down build logs seed migrate frontend clean

## Levanta todos los servicios con Docker Compose
up:
	docker-compose up --build -d
	@echo "✅ Servicios levantados:"
	@echo "   Frontend:          http://localhost:3000"
	@echo "   EventService API:  http://localhost:5000/swagger"
	@echo "   Notification API:  http://localhost:5001/health"
	@echo "   RabbitMQ UI:       http://localhost:15672  (guest/guest)"
	@echo "   PostgreSQL:        localhost:5432"
	@echo "   MongoDB:           localhost:27017"
	@echo "   Redis:             localhost:6379"

## Para todos los servicios
down:
	docker-compose down

## Para y elimina volúmenes (reset completo)
clean:
	docker-compose down -v --remove-orphans

## Ver logs de todos los servicios
logs:
	docker-compose logs -f

## Ver logs solo del EventService
logs-event:
	docker-compose logs -f event-service

## Ver logs solo del NotificationService
logs-notification:
	docker-compose logs -f notification-service

## Ejecutar migraciones EF Core (requiere .NET 9 SDK instalado)
migrate:
	cd src/EventService && dotnet ef database update

## Crear nueva migración (uso: make migration NAME=NombreMigracion)
migration:
	cd src/EventService && dotnet ef migrations add $(NAME)

## Instalar dependencias del frontend
frontend-install:
	cd src/Frontend && npm install

## Correr frontend en modo desarrollo
frontend-dev:
	cd src/Frontend && npm run dev

## Build del frontend
frontend-build:
	cd src/Frontend && npm run build

## Ejecutar EventService localmente (sin Docker)
run-event:
	cd src/EventService && dotnet run

## Ejecutar NotificationService localmente (sin Docker)
run-notification:
	cd src/NotificationService && dotnet run

## Test de la API con curl
test-api:
	@echo "== GET /health =="
	curl -s http://localhost:5000/health | python3 -m json.tool
	@echo ""
	@echo "== GET /api/events =="
	curl -s http://localhost:5000/api/events | python3 -m json.tool

## Crear evento de prueba
test-create:
	curl -s -X POST http://localhost:5000/api/events \
	  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDEiLCJuYW1lIjoiQWRtaW4iLCJyb2xlIjoiYWRtaW4iLCJpc3MiOiJldmVudC1wbGF0Zm9ybSIsImF1ZCI6ImV2ZW50LXBsYXRmb3JtLWNsaWVudHMiLCJleHAiOjk5OTk5OTk5OTl9.signature" \
	  -H "Content-Type: application/json" \
	  -d '{"name":"Festival Test","date":"2027-06-15T20:00:00Z","location":"Lima, Peru","zones":[{"name":"General","price":50,"capacity":1000}]}' \
	  | python3 -m json.tool
