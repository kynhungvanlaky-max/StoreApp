/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Views/**/*.cshtml',
    './Pages/**/*.cshtml',
    './wwwroot/**/*.html'
  ],
  theme: {
    extend: {},
  },
  plugins: [
    require('daisyui')
  ],
  daisyui: {
    themes: ["light", "dark"], // Backend admin có thể dùng light/dark
  }
}
