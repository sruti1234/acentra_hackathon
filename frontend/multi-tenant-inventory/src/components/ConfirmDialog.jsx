import { AlertIcon } from './Icons'

export default function ConfirmDialog({ title, message, confirmLabel = 'Delete', busy, onConfirm, onCancel }) {
  return (
    <div className="modal-backdrop" onMouseDown={busy ? undefined : onCancel}>
      <div className="modal modal-sm" role="alertdialog" aria-modal="true" onMouseDown={(e) => e.stopPropagation()}>
        <div className="confirm-icon">
          <AlertIcon size={22} />
        </div>
        <h2>{title}</h2>
        <p className="muted">{message}</p>
        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={onCancel} disabled={busy}>
            Cancel
          </button>
          <button className="btn btn-danger" onClick={onConfirm} disabled={busy} autoFocus>
            {busy ? <span className="spinner" /> : null}
            {busy ? 'Deleting…' : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
