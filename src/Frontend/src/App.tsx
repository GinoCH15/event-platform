import { Routes, Route, Link, useLocation } from 'react-router-dom';
import { CreateEventPage } from './pages/CreateEventPage';
import { EventListPage } from './pages/EventListPage';
import { Ticket, PlusCircle, List } from 'lucide-react';
import styles from './App.module.css';

export default function App() {
  const { pathname } = useLocation();

  return (
    <div className={styles.shell}>
      <header className={styles.header}>
        <div className={styles.headerInner}>
          <Link to="/events" className={styles.logo}>
            <Ticket size={22} strokeWidth={1.8} />
            <span>EventPlatform</span>
          </Link>

          <nav className={styles.nav}>
            <Link
              to="/events"
              className={`${styles.navLink} ${pathname === '/events' ? styles.active : ''}`}
            >
              <List size={15} />
              Eventos
            </Link>
            <Link
              to="/events/new"
              className={`${styles.navLink} ${pathname === '/events/new' ? styles.active : ''}`}
            >
              <PlusCircle size={15} />
              Crear Evento
            </Link>
          </nav>
        </div>
      </header>

      <main className={styles.main}>
        <Routes>
          <Route path="/events" element={<EventListPage />} />
          <Route path="/events/new" element={<CreateEventPage />} />
          <Route path="*" element={<EventListPage />} />
        </Routes>
      </main>

      <footer className={styles.footer}>
        <span>Event Platform MVP · .NET 9 + React 18 + RabbitMQ + PostgreSQL + Redis</span>
      </footer>
    </div>
  );
}
