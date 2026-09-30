import { formatMoney } from './format'
import { AlertIcon, BoxIcon, WalletIcon } from './Icons'

export default function SummaryCards({ products, loading }) {
  const total = products.length
  const lowStock = products.filter((p) => p.quantity <= 10).length
  const value = products.reduce((sum, p) => sum + p.quantity * p.price, 0)

  const cards = [
    { label: 'Total Products', value: total, hint: 'Products in this organization', icon: <BoxIcon />, tone: 'blue' },
    {
      label: 'Low Stock',
      value: lowStock,
      hint: lowStock > 0 ? 'Need attention' : 'All items well stocked',
      icon: <AlertIcon />,
      tone: lowStock > 0 ? 'amber' : 'green',
    },
    { label: 'Inventory Value', value: formatMoney(value), hint: 'Total current value', icon: <WalletIcon />, tone: 'green' },
  ]

  return (
    <div className="stats">
      {cards.map((c) => (
        <div key={c.label} className="card stat-card">
          <div className={`stat-icon tone-${c.tone}`}>{c.icon}</div>
          <div className="stat-body">
            <div className="stat-label">{c.label}</div>
            {loading ? <div className="skeleton skeleton-value" /> : <div className="stat-value">{c.value}</div>}
            <div className="stat-hint">{c.hint}</div>
          </div>
        </div>
      ))}
    </div>
  )
}
