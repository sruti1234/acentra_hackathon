import { useCallback, useMemo, useRef, useState } from 'react'
import { ToastContext } from './toast-context'
import { AlertIcon, CheckIcon, XIcon } from './Icons'

// Lightweight toast notifications (replaces browser alerts).
export default function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([])
  const nextId = useRef(1)

  const dismiss = useCallback((id) => setToasts((list) => list.filter((t) => t.id !== id)), [])

  const push = useCallback(
    (type, message) => {
      const id = nextId.current++
      setToasts((list) => [...list, { id, type, message }])
      // Errors stay a little longer so they aren't missed.
      setTimeout(() => dismiss(id), type === 'error' ? 7000 : 3500)
    },
    [dismiss],
  )

  const api = useMemo(
    () => ({
      success: (m) => push('success', m),
      error: (m) => push('error', m),
      info: (m) => push('info', m),
    }),
    [push],
  )

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="toast-stack" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast toast-${t.type}`}>
            <span className="toast-icon">{t.type === 'error' ? <AlertIcon size={18} /> : <CheckIcon size={18} />}</span>
            <span className="toast-message">{t.message}</span>
            <button className="icon-btn" onClick={() => dismiss(t.id)} aria-label="Dismiss">
              <XIcon size={16} />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
