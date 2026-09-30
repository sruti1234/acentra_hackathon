import { useState } from 'react'
import { authApi, getErrorMessage, session } from '../api'
import { BoxIcon, ShieldIcon } from './Icons'

export default function LoginPage({ onLoggedIn }) {
  const [email, setEmail] = useState('demo@example.com')
  const [password, setPassword] = useState('Demo@123')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function handleSubmit(e) {
    e.preventDefault()
    setLoading(true)
    setError('')

    try {
      const result = await authApi.login(email, password)
      session.save(result)
      onLoggedIn()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <div className="brand">
          <div className="brand-logo">
            <BoxIcon size={20} />
          </div>
          <div>
            <div className="brand-name">Multi-Tenant Inventory</div>
            <div className="brand-tagline">Secure inventory management for organizations</div>
          </div>
        </div>

        <div>
          <h1>Sign in</h1>
          <p className="muted">Access your organization's inventory.</p>
        </div>

        <div className="field">
          <label className="field-label" htmlFor="email">Email</label>
          <input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
        </div>

        <div className="field">
          <label className="field-label" htmlFor="password">Password</label>
          <input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <button type="submit" className="btn btn-primary btn-lg btn-block" disabled={loading}>
          {loading && <span className="spinner" />}
          {loading ? 'Signing in…' : 'Sign in'}
        </button>

        <div className="demo-accounts">
          <div className="demo-title">
            <ShieldIcon size={16} /> Demo accounts
          </div>
          <div>
            <code>demo@example.com</code> / <code>Demo@123</code> — all 3 organizations
          </div>
          <div>
            <code>abc@example.com</code> / <code>Abc@123</code> — ABC Corporation only
          </div>
        </div>
      </form>
    </div>
  )
}
