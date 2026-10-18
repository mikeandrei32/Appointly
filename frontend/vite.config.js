import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig, loadEnv } from 'vite'
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const target = env.API_PROXY_TARGET || 'http://localhost:5080'
  return {
    plugins: [react(), tailwindcss()],
    server: { host: 'localhost', port: 5173, strictPort: true,
      proxy: { '/api': { target }, '/openapi': { target } } },
  }
})
