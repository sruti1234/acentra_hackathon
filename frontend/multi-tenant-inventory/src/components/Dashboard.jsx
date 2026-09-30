import { useCallback, useEffect, useRef, useState } from 'react'
import { getErrorMessage, productApi, session } from '../api'
import { useToast } from './toast-context'
import Header from './Header'
import TenantSelector from './TenantSelector'
import SummaryCards from './SummaryCards'
import InventoryTable from './InventoryTable'
import ProductForm from './ProductForm'
import ConfirmDialog from './ConfirmDialog'
import IsolationCheck from './IsolationCheck'
import { BoxIcon, PlusIcon, RefreshIcon } from './Icons'

export default function Dashboard({ onLogout }) {
  const user = session.getUser()
  const tenants = session.getTenants()
  const toast = useToast()

  const [tenantId, setTenantId] = useState(() => session.getTenantId() ?? tenants[0]?.id ?? null)
  const [products, setProducts] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editing, setEditing] = useState(null) // null = closed, {} = new, product = edit
  const [confirmDelete, setConfirmDelete] = useState(null)
  const [deleting, setDeleting] = useState(false)

  // Ignore responses from an organization the user already switched away from.
  const latestRequest = useRef(0)

  const loadProducts = useCallback(async () => {
    const requestId = ++latestRequest.current
    setLoading(true)
    setError('')
    try {
      const data = await productApi.list()
      if (requestId === latestRequest.current) setProducts(data)
    } catch (err) {
      if (requestId === latestRequest.current) {
        setProducts([])
        setError(getErrorMessage(err))
      }
    } finally {
      if (requestId === latestRequest.current) setLoading(false)
    }
  }, [])

  // Reload whenever the selected organization changes.
  useEffect(() => {
    if (tenantId) loadProducts()
  }, [tenantId, loadProducts])

  function switchTenant(id) {
    session.setTenantId(id) // the API client sends this as X-Tenant-ID; the server validates it
    setTenantId(id)
  }

  async function handleDelete() {
    const product = confirmDelete
    setDeleting(true)
    try {
      await productApi.remove(product.id)
      toast.success(`"${product.name}" deleted`)
      setConfirmDelete(null)
      await loadProducts()
    } catch (err) {
      toast.error(getErrorMessage(err))
      setConfirmDelete(null)
    } finally {
      setDeleting(false)
    }
  }

  function handleSaved() {
    setEditing(null)
    loadProducts()
  }

  const tenantName = tenants.find((t) => t.id === tenantId)?.name

  if (tenants.length === 0) {
    return (
      <div className="app-shell">
        <Header user={user} onLogout={onLogout} />
        <main className="container">
          <div className="alert alert-error">Your account does not belong to any organization.</div>
        </main>
      </div>
    )
  }

  const showEmpty = !loading && !error && products.length === 0

  return (
    <div className="app-shell">
      <Header user={user} onLogout={onLogout} />

      <main className="container">
        <div className="page-toolbar">
          <TenantSelector tenants={tenants} selectedId={tenantId} onChange={switchTenant} disabled={loading} />
          <button className="btn btn-primary btn-lg" onClick={() => setEditing({})}>
            <PlusIcon size={18} /> Add Product
          </button>
        </div>

        {error && (
          <div className="alert alert-error">
            <strong>Could not load inventory.</strong> {error}
          </div>
        )}

        <SummaryCards products={products} loading={loading} />

        <section className="card inventory-card">
          <div className="card-header">
            <div>
              <h2 className="section-title">Inventory</h2>
              <p className="muted small">{tenantName}</p>
            </div>
            <button className="btn btn-secondary btn-sm" onClick={loadProducts} disabled={loading}>
              <RefreshIcon size={15} className={loading ? 'spin' : ''} /> Refresh
            </button>
          </div>

          {showEmpty ? (
            <div className="empty-state">
              <div className="empty-icon">
                <BoxIcon size={28} />
              </div>
              <h3>No products yet</h3>
              <p className="muted">Add your first product to start managing inventory.</p>
              <button className="btn btn-primary" onClick={() => setEditing({})}>
                <PlusIcon size={16} /> Add Product
              </button>
            </div>
          ) : (
            !error && (
              <InventoryTable
                products={products}
                loading={loading}
                onEdit={(p) => setEditing(p)}
                onDelete={(p) => setConfirmDelete(p)}
                busyId={deleting ? confirmDelete?.id : null}
              />
            )
          )}
        </section>

        <IsolationCheck />
      </main>

      {editing && (
        <ProductForm
          key={editing.id ?? 'new'}
          product={editing.id ? editing : null}
          onClose={() => setEditing(null)}
          onSaved={handleSaved}
        />
      )}

      {confirmDelete && (
        <ConfirmDialog
          title="Delete product?"
          message={`"${confirmDelete.name}" will be permanently removed from ${tenantName}. This cannot be undone.`}
          busy={deleting}
          onConfirm={handleDelete}
          onCancel={() => setConfirmDelete(null)}
        />
      )}
    </div>
  )
}
