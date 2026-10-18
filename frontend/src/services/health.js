export async function getHealth(signal) {
  const response = await fetch('/api/health', { signal })
  if (!response.ok) throw new Error('The API or database is unavailable. Start the backend and apply migrations, then retry.')
  return response.json()
}
