import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { eventApi } from '../services/eventApi';
import type { EventSummaryDto, PagedResult } from '../types';
import { MapPin, Calendar, Users, PlusCircle, RefreshCw, ChevronLeft, ChevronRight } from 'lucide-react';
import { format } from 'date-fns';
import { es } from 'date-fns/locale';
import styles from './EventListPage.module.css';

const STATUS_MAP: Record<string, { label: string; color: string }> = {
  Draft: { label: 'Borrador', color: '#fbbf24' },
  Published: { label: 'Publicado', color: '#22d3a0' },
  Cancelled: { label: 'Cancelado', color: '#ff5f6d' },
  Finished: { label: 'Finalizado', color: '#8888aa' },
};

export function EventListPage() {
  const [data, setData] = useState<PagedResult<EventSummaryDto> | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const fetchEvents = async (p = page) => {
    setLoading(true);
    setError('');
    try {
      const result = await eventApi.getEvents(p, 12);
      setData(result);
    } catch {
      setError('No se pudo conectar con la API. Asegúrate de que el backend esté corriendo.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchEvents(page); }, [page]);

  const occupancyPct = (e: EventSummaryDto) =>
    e.totalCapacity > 0
      ? Math.round(((e.totalCapacity - e.availableCapacity) / e.totalCapacity) * 100)
      : 0;

  return (
    <div className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1 className={styles.title}>Eventos</h1>
          <p className={styles.subtitle}>
            {data ? `${data.totalCount} evento${data.totalCount !== 1 ? 's' : ''} registrados` : '…'}
          </p>
        </div>
        <div className={styles.headerActions}>
          <button className={styles.refreshBtn} onClick={() => fetchEvents(page)} disabled={loading}>
            <RefreshCw size={14} className={loading ? styles.spin : ''} />
            Actualizar
          </button>
          <Link to="/events/new" className={styles.createBtn}>
            <PlusCircle size={15} />
            Crear Evento
          </Link>
        </div>
      </div>

      {error && (
        <div className={styles.errorBanner}>
          ⚠️ {error}
        </div>
      )}

      {loading && !data ? (
        <div className={styles.skeletonGrid}>
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className={styles.skeleton} />
          ))}
        </div>
      ) : data?.items.length === 0 ? (
        <div className={styles.empty}>
          <div className={styles.emptyIcon}>🎪</div>
          <h3>No hay eventos aún</h3>
          <p>Crea el primer evento de la plataforma</p>
          <Link to="/events/new" className={styles.createBtn}>
            <PlusCircle size={15} />
            Crear Evento
          </Link>
        </div>
      ) : (
        <>
          <div className={styles.grid}>
            {data?.items.map((event) => {
              const status = STATUS_MAP[event.status] ?? { label: event.status, color: '#aaa' };
              const occ = occupancyPct(event);
              return (
                <div key={event.id} className={styles.card}>
                  <div className={styles.cardTop}>
                    <span
                      className={styles.statusBadge}
                      style={{ color: status.color, borderColor: `${status.color}33`, background: `${status.color}11` }}
                    >
                      <span className={styles.statusDot} style={{ background: status.color }} />
                      {status.label}
                    </span>
                  </div>

                  <h3 className={styles.cardTitle}>{event.name}</h3>

                  <div className={styles.cardMeta}>
                    <div className={styles.metaItem}>
                      <Calendar size={13} />
                      <span>{format(new Date(event.date), "d MMM yyyy · HH:mm", { locale: es })}</span>
                    </div>
                    <div className={styles.metaItem}>
                      <MapPin size={13} />
                      <span>{event.location}</span>
                    </div>
                  </div>

                  <div className={styles.capacitySection}>
                    <div className={styles.capacityHeader}>
                      <span className={styles.capacityLabel}>
                        <Users size={12} />
                        Disponibilidad
                      </span>
                      <span className={styles.capacityValue}>
                        {event.availableCapacity.toLocaleString()} / {event.totalCapacity.toLocaleString()}
                      </span>
                    </div>
                    <div className={styles.progressBar}>
                      <div
                        className={styles.progressFill}
                        style={{
                          width: `${occ}%`,
                          background: occ > 90 ? 'var(--error)' : occ > 70 ? 'var(--warning)' : 'var(--success)',
                        }}
                      />
                    </div>
                    <span className={styles.occPct}>{occ}% ocupado</span>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <div className={styles.pagination}>
              <button
                className={styles.pageBtn}
                onClick={() => setPage(p => p - 1)}
                disabled={!data.hasPreviousPage}
              >
                <ChevronLeft size={14} /> Anterior
              </button>
              <span className={styles.pageInfo}>
                {data.page} / {data.totalPages}
              </span>
              <button
                className={styles.pageBtn}
                onClick={() => setPage(p => p + 1)}
                disabled={!data.hasNextPage}
              >
                Siguiente <ChevronRight size={14} />
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
