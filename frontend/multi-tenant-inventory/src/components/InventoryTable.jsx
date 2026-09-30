import { formatMoney, stockStatus } from './format'
import { EditIcon, TrashIcon } from './Icons'
import ProductThumb from './ProductThumb'

function SkeletonRows() {
  return Array.from({ length: 4 }, (_, i) => (
    <tr key={i} className="skeleton-row">
      <td data-label="Product">
        <div className="product-cell">
          <div className="skeleton skeleton-thumb" />
          <div className="skeleton skeleton-text" style={{ width: 110 }} />
        </div>
      </td>
      <td data-label="Description"><div className="skeleton skeleton-text" style={{ width: '80%' }} /></td>
      <td data-label="Quantity" className="num"><div className="skeleton skeleton-text" style={{ width: 30, marginLeft: 'auto' }} /></td>
      <td data-label="Price" className="num"><div className="skeleton skeleton-text" style={{ width: 70, marginLeft: 'auto' }} /></td>
      <td data-label="Status"><div className="skeleton skeleton-pill" /></td>
      <td data-label="Actions"><div className="skeleton skeleton-text" style={{ width: 120 }} /></td>
    </tr>
  ))
}

export default function InventoryTable({ products, loading, onEdit, onDelete, busyId }) {
  return (
    <div className="table-wrap">
      <table className="inventory-table">
        <thead>
          <tr>
            <th>Product</th>
            <th>Description</th>
            <th className="num">Quantity</th>
            <th className="num">Price</th>
            <th>Status</th>
            <th className="actions-col">Actions</th>
          </tr>
        </thead>
        <tbody>
          {loading ? (
            <SkeletonRows />
          ) : (
            products.map((p) => {
              const status = stockStatus(p.quantity)
              const busy = busyId === p.id
              return (
                <tr key={p.id}>
                  <td data-label="Product">
                    <div className="product-cell">
                      <ProductThumb product={p} />
                      <span className="product-name">{p.name}</span>
                    </div>
                  </td>
                  <td data-label="Description" className="desc-cell">
                    {p.description || <span className="muted">—</span>}
                  </td>
                  <td data-label="Quantity" className="num">{p.quantity}</td>
                  <td data-label="Price" className="num">{formatMoney(p.price)}</td>
                  <td data-label="Status">
                    <span className={`badge badge-${status.className}`}>
                      <span className="dot" />
                      {status.label}
                    </span>
                  </td>
                  <td data-label="Actions" className="actions">
                    <button className="btn btn-secondary btn-sm" onClick={() => onEdit(p)} disabled={busy}>
                      <EditIcon size={15} /> Edit
                    </button>
                    <button className="btn btn-danger-ghost btn-sm" onClick={() => onDelete(p)} disabled={busy}>
                      <TrashIcon size={15} /> Delete
                    </button>
                  </td>
                </tr>
              )
            })
          )}
        </tbody>
      </table>
    </div>
  )
}
