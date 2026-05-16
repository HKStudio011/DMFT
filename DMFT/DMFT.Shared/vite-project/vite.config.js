import { defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';
import path from 'path';

export default defineConfig({
  build: {
    outDir: path.resolve(__dirname, '../wwwroot/build'),
    emptyOutDir: true,
    lib: {
      entry: path.resolve(__dirname, 'src/ts/main.ts'),
      formats: ['es'],
      fileName: () => 'assets/main.js'
    },
    rollupOptions: {
      output: {
        assetFileNames: 'assets/styles.css'
      }
    }
  },
  plugins: [tailwindcss()]
});