/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./**/*.{razor,cshtml,html}'],
  theme: {
    extend: {
      colors: {
        twitter: {
          primary: '#1DA1F2',     // Color principal para botones, enlaces, etc.
          background: '#FFFFFF',  // Fondo principal blanco
          textPrimary: '#000000', // Color principal del texto
          textSecondary: '#71767B', // Color secundario del texto
          border: '#EFF3F4',      // Color para bordes y fondos claros
          mutedBackground: '#F5F8FA', // Fondo sutil para elementos secundarios
        },
      },
    },
  },
  plugins: [],
}
