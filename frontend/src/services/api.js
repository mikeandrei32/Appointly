export class ApiError extends Error {
  constructor(message, status) { super(message); this.status = status }
}

async function read(response) {
  if (response.status === 204) return null
  const data = await response.json().catch(() => null)
  if (!response.ok) {
    const message = response.status === 429 ? 'Too many attempts. Please wait a minute and try again.'
      : data?.errors ? Object.values(data.errors).flat().join(' ')
      : data?.detail || data?.title || 'The request failed. Please try again.'
    throw new ApiError(message, response.status)
  }
  return data
}

export async function api(path, { method = 'GET', body, signal } = {}) {
  const headers = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) {
    // Tokens are tied to identity; fetch a fresh one after any login/logout.
    const csrf = await read(await fetch('/api/auth/csrf', { credentials: 'include', signal }))
    headers['X-CSRF-TOKEN'] = csrf.token
  }
  return read(await fetch(`/api${path}`, {
    method, credentials: 'include', headers, signal,
    body: body === undefined ? undefined : JSON.stringify(body),
  }))
}
