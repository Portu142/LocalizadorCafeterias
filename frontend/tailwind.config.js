/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        coffee: {
          50: '#faf6f2',
          100: '#f3ebe3',
          200: '#e6d5c6',
          300: '#d4b8a0',
          400: '#b89272',
          500: '#8a6345',
          600: '#7f5a3e',
          700: '#664733',
          800: '#523a2c',
          900: '#3f2d23',
          950: '#241811',
        },
        cream: {
          50: '#fdfbf8',
          100: '#faf5ee',
          200: '#f2e9dc',
        },
        caramel: {
          400: '#d99a5b',
          500: '#c47f3b',
          600: '#a05f28',
          700: '#7d4a1f',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        display: ['Fraunces', 'Georgia', 'serif'],
      },
      boxShadow: {
        soft: '0 18px 50px -24px rgba(63, 45, 35, 0.35)',
      },
    },
  },
  plugins: [],
};
