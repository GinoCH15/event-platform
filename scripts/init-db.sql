-- =====================================================
-- Event Platform — Script de creación de base de datos
-- PostgreSQL 16+
-- 
-- NOTA: En el proyecto se usa EF Core Migrations.
-- Este script es una referencia del modelo de datos.
-- Para ejecutar migraciones: ver README.md
-- =====================================================

CREATE TABLE IF NOT EXISTS events (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    name            VARCHAR(200)    NOT NULL,
    date            TIMESTAMPTZ     NOT NULL,
    location        VARCHAR(500)    NOT NULL,
    status          VARCHAR(20)     NOT NULL DEFAULT 'Draft',
    organizer_id    UUID            NOT NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ,
    CONSTRAINT pk_events PRIMARY KEY (id),
    CONSTRAINT chk_events_status CHECK (status IN ('Draft','Published','Cancelled','Finished'))
);

CREATE TABLE IF NOT EXISTS zones (
    id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
    event_id            UUID            NOT NULL,
    name                VARCHAR(100)    NOT NULL,
    price               NUMERIC(10,2)   NOT NULL,
    capacity            INTEGER         NOT NULL,
    available_capacity  INTEGER         NOT NULL,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_zones PRIMARY KEY (id),
    CONSTRAINT fk_zones_event FOREIGN KEY (event_id) REFERENCES events(id) ON DELETE CASCADE,
    CONSTRAINT chk_zones_price CHECK (price >= 0),
    CONSTRAINT chk_zones_capacity CHECK (capacity > 0),
    CONSTRAINT chk_zones_available CHECK (available_capacity >= 0 AND available_capacity <= capacity)
);

-- Índices para rendimiento
CREATE INDEX IF NOT EXISTS ix_events_status       ON events (status);
CREATE INDEX IF NOT EXISTS ix_events_date         ON events (date);
CREATE INDEX IF NOT EXISTS ix_events_organizer_id ON events (organizer_id);
CREATE INDEX IF NOT EXISTS ix_zones_event_id      ON zones (event_id);
