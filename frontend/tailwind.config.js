/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Components/**/*.razor',
    './wwwroot/**/*.html'
  ],
  theme: {
    extend: {
      keyframes: {
        shimmer: {
          '0%': { transform: 'translateX(-100%)' },
          '100%': { transform: 'translateX(200%)' },
        }
      },
      animation: {
        shimmer: 'shimmer 3s infinite',
      }
    },
  },
  plugins: [
    require('daisyui')
  ],
  daisyui: {
    themes: [{
      nexus: {
        "primary":   "#4f9eff",
        "secondary": "#7c3aed",
        "accent":    "#f59e0b",
        "neutral":   "#1a2332",
        "base-100":  "#0f1724",
        "base-200":  "#151f2e",
        "base-300":  "#1e2d40",
        "info":      "#66c0f4",
        "success":   "#4ade80",
        "warning":   "#f59e0b",
        "error":     "#ef4444"
      }
    }]
  }
}
