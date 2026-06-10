const defaultTheme = require('tailwindcss/defaultTheme');

module.exports = {
  content: [
    './src/**/*.{ts,html}',
  ],
  theme: {
    extend: {
      colors: {
        gold: {
          primary: '#d4af37',
          'primary-hover': '#c5a031',
          'primary-light': '#ffe088',
          'primary-dim': '#e9c349',
          'primary-dark': '#b8962e',
          bg: '#fdfbf4',
          border: '#f0e6c8',
        },
        'deep-black': '#1a1a1a',
        'dark-surface': '#2a2a2a',
        'dark-border': '#3a3a3a',
        surface: {
          base: '#fcfafa',
          card: '#ffffff',
          hover: '#f5f3f0',
          sidebar: '#1a1a1a',
          divider: '#e8e5e0',
        },
        text: {
          primary: '#1a1a1a',
          secondary: '#71717a',
          muted: '#a1a1aa',
          'on-dark': '#f9fafb',
          'on-gold': '#1a1a1a',
        },
        error: {
          DEFAULT: '#ef4444',
          bg: '#fef2f2',
          border: '#fecaca',
        },
        success: {
          DEFAULT: '#10b981',
          bg: '#f0fdf4',
          border: '#bbf7d0',
        },
        warning: {
          DEFAULT: '#d97706',
          bg: '#fffbeb',
          border: '#fde68a',
        },
        info: {
          DEFAULT: '#2563eb',
          bg: '#eff6ff',
          border: '#bfdbfe',
        },
      },
      fontFamily: {
        sans: ['IBM Plex Sans Arabic', ...defaultTheme.fontFamily.sans],
      },
      borderRadius: {
        sm: '4px',
        md: '8px',
        lg: '12px',
        xl: '16px',
      },
      boxShadow: {
        card: '0 4px 20px rgba(0, 0, 0, 0.04)',
        dropdown: '0 10px 32px rgba(0, 0, 0, 0.08)',
        modal: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1)',
      },
      spacing: {
        sidebar: '280px',
      },
    },
  },
  plugins: [],
};
