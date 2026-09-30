import { useState } from 'react'

// Shows the S3 image through its pre-signed URL, or the product's initial as a fallback
// (also used if the pre-signed URL has expired).
export default function ProductThumb({ product, size = 'sm' }) {
  const [failedUrl, setFailedUrl] = useState(null)
  const url = product?.imageUrl

  if (url && failedUrl !== url) {
    return <img src={url} alt={product.name} className={`thumb thumb-${size}`} onError={() => setFailedUrl(url)} />
  }

  return (
    <div className={`thumb thumb-${size} thumb-placeholder`} aria-hidden>
      {(product?.name || '?').charAt(0).toUpperCase()}
    </div>
  )
}
