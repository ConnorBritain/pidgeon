import type { Config } from "tailwindcss";

export default {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      // Pidgeon Health Brand Colors
      colors: {
        // Primary brand colors
        'ph-cyan': {
          100: '#e6f7ff',
          200: '#9feaff',
          300: '#85e2ff', 
          400: '#69d6ff'
        },
        'ph-blue': {
          50: '#eff8ff',
          100: '#dbeafe',
          300: '#4fbfff',
          400: '#2ea2ff',
          500: '#157ef3',
          600: '#0f63d6'
        },
        'ph-navy': {
          700: '#0e3e66',
          900: '#0b1f33'
        },
        'ph-teal': {
          500: '#10b7c4',
          600: '#0b6d8e'
        },
        
        // Semantic colors
        'ph-success': '#2fbf71',
        'ph-green': {
          50: '#f0fdf4',
          100: '#dcfce7',
          500: '#2fbf71',
          600: '#19995a',
          700: '#0e7b45'
        },
        'ph-danger': '#e75a47',
        'ph-red': {
          500: '#e75a47',
          600: '#cc3a2a',
          700: '#a5281b'
        },
        'ph-warning': '#f5a623',
        'ph-info': '#10b7c4',
        
        // Neutrals
        'ph-black': '#0a0a0a',
        'ph-white': '#ffffff',
        'ph-text-dark': '#1a202c',
        'ph-text-medium': '#2d3748',
        'ph-text-light': '#4a5568',
        'ph-graphite': {
          500: '#2a3642',
          700: '#1e2730'
        },
        'ph-gray': {
          100: '#f3f4f6',
          200: '#e5e7eb',
          300: '#d1d5db',
          400: '#9ca3af',
          500: '#6b7280',
          600: '#4b5563',
          700: '#374151',
          800: '#1f2937',
          900: '#111827'
        }
      },
      
      // Healthcare typography
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
        mono: ['JetBrains Mono', 'Courier New', 'monospace']
      },
      
      fontSize: {
        'desktop-xs': ['11px', '16px'],
        'desktop-sm': ['13px', '20px'], 
        'desktop-base': ['14px', '22px'],
        'desktop-lg': ['16px', '26px'],
        'desktop-xl': ['18px', '28px'],
        'desktop-2xl': ['22px', '32px']
      },
      
      // Healthcare spacing
      spacing: {
        'desktop-xs': '6px',
        'desktop-sm': '12px',
        'desktop-md': '20px',
        'desktop-lg': '32px',
        'desktop-xl': '48px',
        
        'activity-bar': '64px',
        'activity-item': '48px',
        'panel-min': '240px',
        'panel-default': '320px',
        'panel-max': '480px'
      },
      
      // Healthcare shadows
      boxShadow: {
        'ph-sm': '0 1px 2px rgba(0, 0, 0, 0.05)',
        'ph-md': '0 1px 3px rgba(0, 0, 0, 0.1)',
        'ph-lg': '0 2px 6px rgba(0, 0, 0, 0.1)',
        'ph-focus': '0 0 0 3px rgba(21, 126, 243, 0.2)'
      },
      
      // Healthcare animations  
      animation: {
        'ph-spin': 'spin 1s linear infinite',
        'ph-pulse': 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite'
      },
      
      // Healthcare gradients
      backgroundImage: {
        'ph-gradient-wing': 'radial-gradient(ellipse at center, #69d6ff 0%, #2ea2ff 45%, #157ef3 70%, #0f63d6 100%)',
        'ph-gradient-wing-hover': 'radial-gradient(ellipse at center, #85e2ff 0%, #4fbfff 45%, #2ea2ff 70%, #157ef3 100%)',
        'ph-gradient-success': 'radial-gradient(ellipse at center, #7be7b0 0%, #2fbf71 55%, #19995a 100%)',
        'ph-gradient-danger': 'radial-gradient(ellipse at center, #ff8e7a 0%, #e75a47 55%, #cc3a2a 100%)',
        'ph-gradient-subtle': 'linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%)'
      }
    },
  },
  plugins: [],
} satisfies Config;