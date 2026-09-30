import { useEffect, useRef, useState } from 'react'
import { getErrorMessage, productApi } from '../api'
import { useToast } from './toast-context'
import { UploadIcon, XIcon } from './Icons'
import ProductThumb from './ProductThumb'

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
const MAX_BYTES = 5 * 1024 * 1024

function validate(form) {
  const errors = {}
  if (!form.name.trim()) errors.name = 'Product name is required.'
  else if (form.name.length > 200) errors.name = 'Name must be 200 characters or fewer.'
  if (form.description.length > 1000) errors.description = 'Description must be 1000 characters or fewer.'
  if (form.quantity === '' || !Number.isInteger(Number(form.quantity)) || Number(form.quantity) < 0)
    errors.quantity = 'Quantity must be a whole number, 0 or more.'
  if (form.price === '' || Number.isNaN(Number(form.price)) || Number(form.price) < 0)
    errors.price = 'Price must be 0 or more.'
  return errors
}

// Modal used for both "Add Product" and "Edit Product".
export default function ProductForm({ product, onClose, onSaved }) {
  const isEdit = Boolean(product)
  const toast = useToast()
  const fileInput = useRef(null)

  const [form, setForm] = useState({
    name: product?.name ?? '',
    description: product?.description ?? '',
    quantity: product?.quantity ?? 0,
    price: product?.price ?? 0,
  })
  const [touched, setTouched] = useState(false)
  const [file, setFile] = useState(null)
  const [preview, setPreview] = useState(null)
  const [fileError, setFileError] = useState('')
  const [dragging, setDragging] = useState(false)
  const [phase, setPhase] = useState('idle') // idle | saving | uploading
  const [progress, setProgress] = useState(0)
  const [serverError, setServerError] = useState('')

  const errors = validate(form)
  const busy = phase !== 'idle'

  // Local preview of the chosen image (before it's uploaded to S3).
  useEffect(() => {
    if (!file) return undefined
    const url = URL.createObjectURL(file)
    setPreview(url)
    return () => URL.revokeObjectURL(url)
  }, [file])

  const update = (field) => (e) => setForm({ ...form, [field]: e.target.value })

  function pickFile(selected) {
    setFileError('')
    if (!selected) return
    if (!ALLOWED_TYPES.includes(selected.type)) {
      setFileError('Please choose a JPEG, PNG, GIF or WebP image.')
      return
    }
    if (selected.size > MAX_BYTES) {
      setFileError('Image must be 5 MB or smaller.')
      return
    }
    setFile(selected)
  }

  function clearFile() {
    setFile(null)
    setPreview(null)
    if (fileInput.current) fileInput.current.value = ''
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setTouched(true)
    setServerError('')
    if (Object.keys(errors).length > 0) return

    // No tenantId here on purpose: the server takes it from the validated X-Tenant-ID.
    const payload = {
      name: form.name.trim(),
      description: form.description.trim(),
      quantity: Number(form.quantity),
      price: Number(form.price),
    }

    setPhase('saving')
    let saved
    try {
      saved = isEdit ? await productApi.update(product.id, payload) : await productApi.create(payload)
    } catch (err) {
      setServerError(getErrorMessage(err))
      setPhase('idle')
      return
    }

    toast.success(isEdit ? `"${saved.name}" updated` : `"${saved.name}" created`)

    if (file) {
      setPhase('uploading')
      setProgress(0)
      try {
        await productApi.uploadImage(saved.id, file, setProgress)
        toast.success('Image uploaded')
      } catch (err) {
        // The product itself is saved; only the image failed (e.g. S3 unavailable).
        toast.error(`Image upload failed: ${getErrorMessage(err)}`)
      }
    }

    setPhase('idle')
    onSaved()
  }

  const showError = (field) => touched && errors[field]

  return (
    <div className="modal-backdrop" onMouseDown={busy ? undefined : onClose}>
      <form className="modal" onSubmit={handleSubmit} onMouseDown={(e) => e.stopPropagation()} noValidate>
        <div className="modal-header">
          <h2>{isEdit ? 'Edit product' : 'Add product'}</h2>
          <button type="button" className="icon-btn" onClick={onClose} disabled={busy} aria-label="Close">
            <XIcon size={18} />
          </button>
        </div>

        <div className="field">
          <label className="field-label" htmlFor="pf-name">Name</label>
          <input id="pf-name" value={form.name} onChange={update('name')} maxLength={200} autoFocus
            className={showError('name') ? 'invalid' : ''} placeholder="e.g. Wireless Mouse" />
          {showError('name') && <div className="field-error">{errors.name}</div>}
        </div>

        <div className="field">
          <label className="field-label" htmlFor="pf-desc">Description</label>
          <textarea id="pf-desc" value={form.description} onChange={update('description')} maxLength={1000} rows={3}
            className={showError('description') ? 'invalid' : ''} placeholder="Optional short description" />
          {showError('description') && <div className="field-error">{errors.description}</div>}
        </div>

        <div className="field-row">
          <div className="field">
            <label className="field-label" htmlFor="pf-qty">Quantity</label>
            <input id="pf-qty" type="number" min="0" step="1" value={form.quantity} onChange={update('quantity')}
              className={showError('quantity') ? 'invalid' : ''} />
            {showError('quantity') && <div className="field-error">{errors.quantity}</div>}
          </div>
          <div className="field">
            <label className="field-label" htmlFor="pf-price">Price (₹)</label>
            <input id="pf-price" type="number" min="0" step="0.01" value={form.price} onChange={update('price')}
              className={showError('price') ? 'invalid' : ''} />
            {showError('price') && <div className="field-error">{errors.price}</div>}
          </div>
        </div>

        <div className="field">
          <span className="field-label">Product image</span>
          <div className="image-area">
            <div className="image-preview">
              {preview ? (
                <img src={preview} alt="Selected" className="thumb thumb-lg" />
              ) : (
                <ProductThumb product={product ?? { name: form.name }} size="lg" />
              )}
              <span className="image-caption">
                {preview ? 'New image (not uploaded yet)' : product?.imageUrl ? 'Current image' : 'No image'}
              </span>
            </div>

            <div
              className={`dropzone ${dragging ? 'dragging' : ''}`}
              onClick={() => !busy && fileInput.current?.click()}
              onDragOver={(e) => { e.preventDefault(); setDragging(true) }}
              onDragLeave={() => setDragging(false)}
              onDrop={(e) => { e.preventDefault(); setDragging(false); if (!busy) pickFile(e.dataTransfer.files[0]) }}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && fileInput.current?.click()}
            >
              <UploadIcon size={22} />
              <div>
                <strong>Click to choose</strong> or drag an image here
              </div>
              <div className="muted small">JPEG, PNG, GIF or WebP · max 5 MB · stored privately in AWS S3</div>
              {file && (
                <div className="file-chip" onClick={(e) => e.stopPropagation()}>
                  {file.name}
                  <button type="button" className="icon-btn" onClick={clearFile} disabled={busy} aria-label="Remove image">
                    <XIcon size={14} />
                  </button>
                </div>
              )}
            </div>
            <input ref={fileInput} type="file" accept={ALLOWED_TYPES.join(',')} hidden
              onChange={(e) => pickFile(e.target.files[0])} />
          </div>
          {fileError && <div className="field-error">{fileError}</div>}
        </div>

        {phase === 'uploading' && (
          <div className="progress">
            <div className="progress-label">Uploading image… {progress}%</div>
            <div className="progress-track"><div className="progress-bar" style={{ width: `${progress}%` }} /></div>
          </div>
        )}

        {serverError && <div className="alert alert-error">{serverError}</div>}

        <div className="modal-actions">
          <button type="button" className="btn btn-secondary" onClick={onClose} disabled={busy}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" disabled={busy}>
            {busy && <span className="spinner" />}
            {phase === 'saving' ? 'Saving…' : phase === 'uploading' ? 'Uploading…' : isEdit ? 'Save changes' : 'Add product'}
          </button>
        </div>
      </form>
    </div>
  )
}
