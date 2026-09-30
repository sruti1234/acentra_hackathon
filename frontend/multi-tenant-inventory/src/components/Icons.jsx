// Small inline SVG icon set (no extra dependencies).
const base = {
  width: 20,
  height: 20,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 2,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
  'aria-hidden': true,
}

const make = (paths) =>
  function Icon({ size = 20, ...props }) {
    return (
      <svg {...base} width={size} height={size} {...props}>
        {paths}
      </svg>
    )
  }

export const BoxIcon = make(
  <>
    <path d="M21 8 12 3 3 8v8l9 5 9-5V8Z" />
    <path d="m3 8 9 5 9-5" />
    <path d="M12 13v8" />
  </>,
)
export const AlertIcon = make(
  <>
    <path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />
    <path d="M12 9v4" />
    <path d="M12 17h.01" />
  </>,
)
export const WalletIcon = make(
  <>
    <rect x="2" y="6" width="20" height="14" rx="2" />
    <path d="M2 10h20" />
    <path d="M16 15h2" />
  </>,
)
export const BuildingIcon = make(
  <>
    <rect x="4" y="2" width="16" height="20" rx="2" />
    <path d="M9 22v-4h6v4" />
    <path d="M8 6h.01M12 6h.01M16 6h.01M8 10h.01M12 10h.01M16 10h.01M8 14h.01M12 14h.01M16 14h.01" />
  </>,
)
export const ShieldIcon = make(
  <>
    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" />
    <path d="m9 12 2 2 4-4" />
  </>,
)
export const PlusIcon = make(<path d="M12 5v14M5 12h14" />)
export const EditIcon = make(
  <>
    <path d="M12 20h9" />
    <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" />
  </>,
)
export const TrashIcon = make(
  <>
    <path d="M3 6h18" />
    <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
    <path d="M19 6 18 20a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
  </>,
)
export const LogoutIcon = make(
  <>
    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
    <path d="m16 17 5-5-5-5" />
    <path d="M21 12H9" />
  </>,
)
export const UploadIcon = make(
  <>
    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
    <path d="m17 8-5-5-5 5" />
    <path d="M12 3v12" />
  </>,
)
export const RefreshIcon = make(
  <>
    <path d="M21 12a9 9 0 1 1-3-6.7L21 8" />
    <path d="M21 3v5h-5" />
  </>,
)
export const CheckIcon = make(<path d="M20 6 9 17l-5-5" />)
export const XIcon = make(<path d="M18 6 6 18M6 6l12 12" />)
export const LockIcon = make(
  <>
    <rect x="3" y="11" width="18" height="11" rx="2" />
    <path d="M7 11V7a5 5 0 0 1 10 0v4" />
  </>,
)
