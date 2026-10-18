import { useCallback, useEffect, useState } from 'react'
import { api } from '../services/api'
import { AuthContext } from './AuthContext'

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const loadSession = useCallback((signal) => api('/auth/me', { signal })
    .then((account) => { setUser(account); setError('') })
    .catch((failure) => {
      if (failure.name === 'AbortError') return
      if (failure.status === 401) { setUser(null); setError('') }
      else setError('Unable to check your session. Check your connection and retry.')
    })
    .finally(() => { if (!signal?.aborted) setLoading(false) }), [])
  useEffect(() => {
    const controller = new AbortController()
    loadSession(controller.signal)
    return () => controller.abort()
  }, [loadSession])
  async function refresh() {
    setLoading(true)
    setError('')
    await loadSession()
  }
  async function login(values) {
    const account = await api('/auth/login', { method: 'POST', body: values })
    setUser(account)
    setError('')
    return account
  }
  async function logout() {
    try { await api('/auth/logout', { method: 'POST' }) }
    catch (failure) { if (failure.status !== 401) throw failure }
    setUser(null)
  }
  return <AuthContext.Provider value={{ user, loading, error, refresh, login, logout }}>{children}</AuthContext.Provider>
}
