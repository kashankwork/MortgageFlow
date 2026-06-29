import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { canCreateLoan, canUsePersonalQueue, canUseTeamQueue } from '../auth/roleAccess'

export function AppShell() {
  const { user, logout } = useAuth()

  return (
    <div className="app-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">MortgageFlow</p>
          <h1>Role-based workflow</h1>
        </div>
        {user ? (
          <div className="user-card">
            <span>{user.fullName}</span>
            <small>{user.role}</small>
            <button type="button" className="secondary" onClick={logout}>
              Sign out
            </button>
          </div>
        ) : null}
      </header>

      {user ? (
        <nav className="nav-links" aria-label="Primary navigation">
          <NavLink to="/loans">Loans</NavLink>
          {canCreateLoan(user.role) ? <NavLink to="/loans/new">New loan</NavLink> : null}
          {canUsePersonalQueue(user.role) ? <NavLink to="/queues/me">My queue</NavLink> : null}
          {canUseTeamQueue(user.role) ? <NavLink to="/queues/team">Team queue</NavLink> : null}
        </nav>
      ) : null}

      <main className="main-content">
        <Outlet />
      </main>
    </div>
  )
}
