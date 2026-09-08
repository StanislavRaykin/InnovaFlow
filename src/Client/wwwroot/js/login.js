
    tailwind.config = {
      theme: {
        extend: {
          colors: {
            brand: {
              darkest: '#0b1320',
              dark: '#111827',
              navy: '#1a2235',
              mint: '#5eead4',
              mintDark: '#14b8a6',
              textMuted: '#9ca3af',
              border: '#374151'
            }
          },
          fontFamily: {
            sans: ['Inter', 'ui-sans-serif', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
            mono: ['ui-monospace', 'SFMono-Regular', 'Menlo', 'Monaco', 'Consolas', 'Liberation Mono', 'Courier New', 'monospace'],
          },
          backgroundImage: {
            'grid-pattern': "linear-gradient(to right, rgba(255,255,255,0.05) 1px, transparent 1px), linear-gradient(to bottom, rgba(255,255,255,0.05) 1px, transparent 1px)",
          }
        }
      }
    }