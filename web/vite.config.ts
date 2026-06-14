import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import basicSsl from '@vitejs/plugin-basic-ssl'
import { defineConfig } from 'vite'

const standaloneHttps = process.env.VITE_DEV_HTTPS === '1' || process.env.VITE_DEV_HTTPS === 'true'

export default defineConfig({
  plugins: [react(), tailwindcss(), ...(standaloneHttps ? [basicSsl()] : [])],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5000,
    strictPort: true,
  },
})
