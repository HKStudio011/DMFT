/**  @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    '../Layout/**/*.razor',
    '../Layout/**/*.razor.css',
    '../Pages/**/*.razor',
    '../Pages/**/*.razor.css',
    '../Components/**/*.razor',
    '../Components/**/*.razor.css',
    '../Shared/**/*.razor',
    '../Shared/**/*.razor.css',
    '../wwwroot/index.html',
  ],
  safelist: [],
  theme: {
      extend: {},
  },
  plugins: [],
}
