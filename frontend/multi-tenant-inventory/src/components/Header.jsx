import { BoxIcon, LogoutIcon } from './Icons'

export default function Header({ user, onLogout }) {
  const initials = (user?.name || '?')
    .split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()

  return (
    <header className="app-header">
      <div className="header-inner">
        <div className="brand">
          <div className="brand-logo">
            <BoxIcon size={20} />
          </div>
          <div>
            <div className="brand-name">Multi-Tenant Inventory</div>
            <div className="brand-tagline">Secure inventory management for organizations</div>
          </div>
        </div>

        <div className="header-user">
          <div className="avatar" aria-hidden>
            {initials}
          </div>
          <div className="user-text">
            <div className="user-name">{user?.name}</div>
            <div className="user-email">{user?.email}</div>
          </div>
          <button className="btn btn-secondary btn-sm" onClick={onLogout}>
            <LogoutIcon size={16} />
            <span>Logout</span>
          </button>
        </div>
      </div>
    </header>
  )
}
