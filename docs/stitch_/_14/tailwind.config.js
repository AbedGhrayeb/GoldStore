tailwind.config = {
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        arabic: ['IBM Plex Sans Arabic', 'sans-serif'],
      },
      colors: {
        gold:            '#735c00',
        'gold-light':    '#e9c349',
        'gold-bright':   '#d4af37',
        'primary-cont':  '#d4af37',
        'on-primary-cont': '#241a00',
        surface:         '#f4f2fd',
        'surface-card':  '#ffffff',
        'on-surface':    '#1a1b22',
        muted:           '#5f5e5e',
        'outline-var':   '#d0c5af',
        outline:         '#7f7663',
        tertiary:        '#006c49',
        error:           '#ba1a1a',
      },
      borderRadius: {
        xl: '0.75rem',
        '2xl': '1rem',
      },
    },
  },
};
