import { useEffect, useState } from 'react'
import { api } from '../services/api'

export function useApiData(path) {
  const [result, setResult] = useState(null)
  const [failure, setFailure] = useState(null)
  const [version, setVersion] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    api(path, { signal: controller.signal })
      .then((data) => {
        if (!controller.signal.aborted) { setResult({ path, data }); setFailure(null) }
      })
      .catch((error) => {
        if (!controller.signal.aborted) { setResult(null); setFailure({ path, message: error.message }) }
      })
    return () => controller.abort()
  }, [path, version])
  function reload() { setResult(null); setFailure(null); setVersion((value) => value + 1) }
  return {
    data: result?.path === path ? result.data : null,
    error: failure?.path === path ? failure.message : '',
    reload,
  }
}
