const money = new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })

export const formatMoney = (value) => money.format(value)

export function stockStatus(quantity) {
  if (quantity === 0) return { label: 'Out of Stock', className: 'out' }
  if (quantity <= 10) return { label: 'Low Stock', className: 'low' }
  return { label: 'In Stock', className: 'ok' }
}
