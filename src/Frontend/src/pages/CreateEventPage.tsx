import { useState } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { eventApi } from '../services/eventApi';
import type { CreateEventInput } from '../types';
import { Plus, Trash2, Loader2, CheckCircle2, AlertCircle, MapPin, Calendar, Tag, Users, DollarSign } from 'lucide-react';
import styles from './CreateEventPage.module.css';

export function CreateEventPage() {
  const navigate = useNavigate();
  const [submitState, setSubmitState] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
  const [errorMsg, setErrorMsg] = useState('');

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<CreateEventInput>({
    defaultValues: {
      name: '',
      date: '',
      location: '',
      zones: [{ name: '', price: 0, capacity: 1 }],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'zones' });

  const onSubmit = async (data: CreateEventInput) => {
    setSubmitState('loading');
    setErrorMsg('');
    try {
      await eventApi.createEvent({
        ...data,
        date: new Date(data.date).toISOString(),
        zones: data.zones.map(z => ({
          ...z,
          price: Number(z.price),
          capacity: Number(z.capacity),
        })),
      });
      setSubmitState('success');
      setTimeout(() => navigate('/events'), 2000);
    } catch (err: any) {
      setSubmitState('error');
      const apiError = err.response?.data;
      if (apiError?.error) setErrorMsg(apiError.error);
      else if (apiError?.errors) {
        const msgs = Object.values(apiError.errors as Record<string, string[]>).flat();
        setErrorMsg(msgs.join(' · '));
      } else {
        setErrorMsg('Error al conectar con el servidor. Verifica que la API esté corriendo.');
      }
    }
  };

  const minDate = new Date();
  minDate.setDate(minDate.getDate() + 1);
  const minDateStr = minDate.toISOString().slice(0, 16);

  return (
    <div className={styles.page}>
      <div className={styles.pageHeader}>
        <div className={styles.badge}>Nuevo Evento</div>
        <h1 className={styles.title}>Registrar Evento</h1>
        <p className={styles.subtitle}>
          Completa la información del evento y define las zonas con su precio y capacidad.
          El evento se publicará al broker de mensajería automáticamente.
        </p>
      </div>

      {submitState === 'success' && (
        <div className={`${styles.alert} ${styles.alertSuccess}`}>
          <CheckCircle2 size={18} />
          <div>
            <strong>¡Evento creado!</strong> Redirigiendo a la lista de eventos…
          </div>
        </div>
      )}

      {submitState === 'error' && (
        <div className={`${styles.alert} ${styles.alertError}`}>
          <AlertCircle size={18} />
          <div>
            <strong>Error:</strong> {errorMsg}
          </div>
        </div>
      )}

      <form className={styles.form} onSubmit={handleSubmit(onSubmit)} noValidate>
        {/* ─── Información Principal ─── */}
        <section className={styles.section}>
          <div className={styles.sectionHeader}>
            <span className={styles.sectionNum}>01</span>
            <h2 className={styles.sectionTitle}>Información Principal</h2>
          </div>

          <div className={styles.grid2}>
            <div className={styles.field}>
              <label className={styles.label}>
                <Tag size={13} />
                Nombre del Evento *
              </label>
              <input
                className={`${styles.input} ${errors.name ? styles.inputError : ''}`}
                placeholder="ej. Festival de Jazz 2026"
                {...register('name', {
                  required: 'El nombre es requerido',
                  maxLength: { value: 200, message: 'Máximo 200 caracteres' },
                })}
              />
              {errors.name && <span className={styles.error}>{errors.name.message}</span>}
            </div>

            <div className={styles.field}>
              <label className={styles.label}>
                <Calendar size={13} />
                Fecha y Hora *
              </label>
              <input
                type="datetime-local"
                min={minDateStr}
                className={`${styles.input} ${errors.date ? styles.inputError : ''}`}
                {...register('date', {
                  required: 'La fecha es requerida',
                  validate: (v) =>
                    new Date(v) > new Date() || 'La fecha debe ser futura',
                })}
              />
              {errors.date && <span className={styles.error}>{errors.date.message}</span>}
            </div>
          </div>

          <div className={styles.field}>
            <label className={styles.label}>
              <MapPin size={13} />
              Ubicación *
            </label>
            <input
              className={`${styles.input} ${errors.location ? styles.inputError : ''}`}
              placeholder="ej. Estadio Nacional, Lima, Perú"
              {...register('location', {
                required: 'La ubicación es requerida',
                maxLength: { value: 500, message: 'Máximo 500 caracteres' },
              })}
            />
            {errors.location && <span className={styles.error}>{errors.location.message}</span>}
          </div>
        </section>

        {/* ─── Zonas ─── */}
        <section className={styles.section}>
          <div className={styles.sectionHeader}>
            <span className={styles.sectionNum}>02</span>
            <h2 className={styles.sectionTitle}>Zonas del Evento</h2>
            <span className={styles.sectionMeta}>{fields.length} / 20 zonas</span>
          </div>

          {errors.zones?.root && (
            <span className={styles.error}>{errors.zones.root.message}</span>
          )}

          <div className={styles.zonesList}>
            {fields.map((field, index) => (
              <div key={field.id} className={styles.zoneCard}>
                <div className={styles.zoneCardHeader}>
                  <span className={styles.zoneIndex}>Zona {index + 1}</span>
                  {fields.length > 1 && (
                    <button
                      type="button"
                      className={styles.removeBtn}
                      onClick={() => remove(index)}
                      title="Eliminar zona"
                    >
                      <Trash2 size={14} />
                    </button>
                  )}
                </div>

                <div className={styles.grid3}>
                  <div className={styles.field}>
                    <label className={styles.label}>
                      <Tag size={12} />
                      Nombre *
                    </label>
                    <input
                      className={`${styles.input} ${errors.zones?.[index]?.name ? styles.inputError : ''}`}
                      placeholder="ej. Campo, VIP, Palco"
                      {...register(`zones.${index}.name`, {
                        required: 'Requerido',
                        maxLength: { value: 100, message: 'Máx 100' },
                      })}
                    />
                    {errors.zones?.[index]?.name && (
                      <span className={styles.error}>{errors.zones[index]!.name!.message}</span>
                    )}
                  </div>

                  <div className={styles.field}>
                    <label className={styles.label}>
                      <DollarSign size={12} />
                      Precio (S/.) *
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      className={`${styles.input} ${errors.zones?.[index]?.price ? styles.inputError : ''}`}
                      placeholder="0.00"
                      {...register(`zones.${index}.price`, {
                        required: 'Requerido',
                        min: { value: 0, message: 'Precio ≥ 0' },
                        valueAsNumber: true,
                      })}
                    />
                    {errors.zones?.[index]?.price && (
                      <span className={styles.error}>{errors.zones[index]!.price!.message}</span>
                    )}
                  </div>

                  <div className={styles.field}>
                    <label className={styles.label}>
                      <Users size={12} />
                      Capacidad *
                    </label>
                    <input
                      type="number"
                      min="1"
                      className={`${styles.input} ${errors.zones?.[index]?.capacity ? styles.inputError : ''}`}
                      placeholder="500"
                      {...register(`zones.${index}.capacity`, {
                        required: 'Requerido',
                        min: { value: 1, message: 'Capacidad > 0' },
                        valueAsNumber: true,
                      })}
                    />
                    {errors.zones?.[index]?.capacity && (
                      <span className={styles.error}>{errors.zones[index]!.capacity!.message}</span>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>

          {fields.length < 20 && (
            <button
              type="button"
              className={styles.addZoneBtn}
              onClick={() => append({ name: '', price: 0, capacity: 1 })}
            >
              <Plus size={15} />
              Agregar Zona
            </button>
          )}
        </section>

        {/* ─── Submit ─── */}
        <div className={styles.actions}>
          <button
            type="button"
            className={styles.cancelBtn}
            onClick={() => navigate('/events')}
          >
            Cancelar
          </button>
          <button
            type="submit"
            className={styles.submitBtn}
            disabled={submitState === 'loading' || submitState === 'success'}
          >
            {submitState === 'loading' ? (
              <>
                <Loader2 size={16} className={styles.spin} />
                Guardando…
              </>
            ) : submitState === 'success' ? (
              <>
                <CheckCircle2 size={16} />
                ¡Guardado!
              </>
            ) : (
              'Guardar Evento'
            )}
          </button>
        </div>
      </form>
    </div>
  );
}
