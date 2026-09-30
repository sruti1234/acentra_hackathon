import axios from 'axios'

export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

const TOKEN_KEY = 'mti.token'
const USER_KEY = 'mti.user'
const TENANTS_KEY = 'mti.tenants'
const TENANT_ID_KEY = 'mti.tenantId'

// ---------- Session storage helpers ----------

export const session = {
  getToken: () => localStorage.getItem(TOKEN_KEY),
  getUser: () => JSON.parse(localStorage.getItem(USER_KEY) || 'null'),
  getTenants: () => JSON.parse(localStorage.getItem(TENANTS_KEY) || '[]'),
  getTenantId: () => Number(localStorage.getItem(TENANT_ID_KEY)) || null,

  save({ token, user, tenants }) {
    localStorage.setItem(TOKEN_KEY, token)
    localStorage.setItem(USER_KEY, JSON.stringify(user))
    localStorage.setItem(TENANTS_KEY, JSON.stringify(tenants))
    // Select the first organization automatically.
    if (tenants.length > 0) localStorage.setItem(TENANT_ID_KEY, String(tenants[0].id))
  },

  setTenantId(id) {
    localStorage.setItem(TENANT_ID_KEY, String(id))
  },

  clear() {
    ;[TOKEN_KEY, USER_KEY, TENANTS_KEY, TENANT_ID_KEY].forEach((k) => localStorage.removeItem(k))
  },
}

// ---------- Central API client ----------

export const api = axios.create({ baseURL: API_BASE_URL })

// Every request automatically carries the JWT and the selected organization.
// NOTE: this is convenience only — the backend re-checks membership on every request.
api.interceptors.request.use((config) => {
  const token = session.getToken()
  const tenantId = session.getTenantId()

  if (token) config.headers.Authorization = `Bearer ${token}`
  if (tenantId && !config.headers['X-Tenant-ID']) config.headers['X-Tenant-ID'] = tenantId

  return config
})

// 401 anywhere (except the login call) = session expired -> back to login.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const isLogin = error.config?.url?.includes('/api/auth/login')
    if (error.response?.status === 401 && !isLogin) {
      session.clear()
      window.dispatchEvent(new Event('auth:logout'))
    }
    return Promise.reject(error)
  },
)

// Turns any API error into a message for the user.
export function getErrorMessage(error) {
  if (!error?.response) return 'Cannot reach the server. Is the API running?'

  const { status, data } = error.response

  switch (status) {
    case 400:
      if (data?.errors) return Object.values(data.errors).flat().join(' ')
      return data?.error || 'Invalid request.'
    case 401:
      return data?.error || 'Your session has expired. Please log in again.'
    case 403:
      return 'You do not have access to this organization.'
    case 404:
      return data?.error || 'Not found.'
    case 502:
    case 503:
      return data?.error || 'A required service is unavailable.'
    default:
      return 'Something went wrong on the server. Please try again.'
  }
}

// ---------- Endpoints ----------

export const authApi = {
  login: (email, password) => api.post('/api/auth/login', { email, password }).then((r) => r.data),
}

export const productApi = {
  list: () => api.get('/api/products').then((r) => r.data),
  create: (product) => api.post('/api/products', product).then((r) => r.data),
  update: (id, product) => api.put(`/api/products/${id}`, product).then((r) => r.data),
  remove: (id) => api.delete(`/api/products/${id}`),
  uploadImage: (id, file, onProgress) => {
    const form = new FormData()
    form.append('file', file)
    return api
      .post(`/api/products/${id}/image`, form, {
        onUploadProgress: (e) => onProgress?.(e.total ? Math.round((e.loaded / e.total) * 100) : 0),
      })
      .then((r) => r.data)
  },
}
