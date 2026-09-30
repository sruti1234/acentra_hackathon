import { BuildingIcon } from './Icons'

export default function TenantSelector({ tenants, selectedId, onChange, disabled }) {
  return (
    <div className="tenant-selector">
      <div className="tenant-icon">
        <BuildingIcon size={20} />
      </div>
      <div className="tenant-field">
        <label htmlFor="tenant-select" className="field-label">
          Organization
        </label>
        <select
          id="tenant-select"
          value={selectedId ?? ''}
          onChange={(e) => onChange(Number(e.target.value))}
          disabled={disabled}
        >
          {tenants.map((t) => (
            <option key={t.id} value={t.id}>
              {t.name}
            </option>
          ))}
        </select>
      </div>
      <span className="chip chip-active">
        <span className="dot" /> Active organization
      </span>
    </div>
  )
}
