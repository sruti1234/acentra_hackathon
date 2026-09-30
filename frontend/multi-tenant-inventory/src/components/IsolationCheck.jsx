import { useState } from 'react'
import { api, getErrorMessage } from '../api'
import { CheckIcon, LockIcon, ShieldIcon } from './Icons'

// Demo: sends GET /api/products with ANY X-Tenant-ID you type,
// to show that the backend (not React) decides what you may access.
export default function IsolationCheck() {
  const [tenantId, setTenantId] = useState('2')
  const [result, setResult] = useState(null)
  const [loading, setLoading] = useState(false)

  async function run(e) {
    e.preventDefault()
    setLoading(true)
    setResult(null)
    try {
      const res = await api.get('/api/products', { headers: { 'X-Tenant-ID': tenantId } })
      setResult({
        ok: true,
        title: '200 — Access granted',
        text: `You are a member of tenant ${tenantId}. ${res.data.length} product(s): ${res.data.map((p) => p.name).join(', ') || 'none'}`,
      })
    } catch (err) {
      const status = err.response?.status
      setResult({
        ok: false,
        title: status === 403 ? '403 — Access denied' : status ? `${status} — Request rejected` : 'Request failed',
        text: getErrorMessage(err),
      })
    } finally {
      setLoading(false)
    }
  }

  return (
    <section className="card security-card">
      <div className="security-head">
        <div className="stat-icon tone-blue">
          <ShieldIcon />
        </div>
        <div>
          <h2 className="section-title">Tenant Isolation</h2>
          <p className="muted">Verify that users can access only organizations they are authorized to access.</p>
        </div>
      </div>

      <form className="security-form" onSubmit={run}>
        <div className="field">
          <label className="field-label" htmlFor="iso-tenant">Tenant ID</label>
          <input id="iso-tenant" value={tenantId} onChange={(e) => setTenantId(e.target.value)} placeholder="e.g. 2" />
        </div>
        <button className="btn btn-primary" disabled={loading || !tenantId.trim()}>
          {loading && <span className="spinner" />}
          {loading ? 'Sending…' : 'Send Request'}
        </button>
      </form>
      <p className="muted small">
        Sends <code>GET /api/products</code> with header <code>X-Tenant-ID: {tenantId || '…'}</code>. The server checks your membership.
      </p>

      {result && (
        <div className={`security-result ${result.ok ? 'ok' : 'denied'}`}>
          <span className="security-result-icon">{result.ok ? <CheckIcon size={18} /> : <LockIcon size={18} />}</span>
          <div>
            <div className="security-result-title">{result.title}</div>
            <div>{result.text}</div>
          </div>
        </div>
      )}
    </section>
  )
}
