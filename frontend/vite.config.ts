import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        // Port 44339 = IIS Express HTTPS | 7221 = dotnet run HTTPS
        // Must target HTTPS directly — HTTP→HTTPS 307 redirect strips the
        // Authorization header in browsers, causing 401 on all protected endpoints.
        target: 'https://localhost:44339',
        changeOrigin: true,
        secure: false, // allow self-signed IIS Express cert
      },
    },
  },
})
