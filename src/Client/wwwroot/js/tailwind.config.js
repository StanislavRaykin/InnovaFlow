// Tailwind Play CDN configuration, shared by every page in the app.
// Loaded from index.html immediately after the CDN script.
//
// Token groups are namespaced per design so they never collide:
//   Landing      -> Material-style semantic tokens (surface / primary / on-* ...)
//   Login        -> brand.*
//   Registration -> innova-*
tailwind.config = {
    darkMode: "class",
    theme: {
        extend: {
            colors: {
                // --- Landing ---------------------------------------------
                "surface": "#0b1326",
                "on-primary-container": "#005e2d",
                "secondary-fixed-dim": "#2fd9f4",
                "surface-dim": "#0b1326",
                "on-tertiary-container": "#624f00",
                "on-tertiary-fixed-variant": "#574500",
                "surface-container-lowest": "#060e20",
                "tertiary-fixed": "#ffe083",
                "primary-fixed-dim": "#4de082",
                "on-primary-fixed": "#00210c",
                "on-tertiary": "#3c2f00",
                "on-primary": "#003919",
                "surface-container-high": "#222a3d",
                "secondary": "#5de6ff",
                "inverse-primary": "#006d36",
                "surface-variant": "#2d3449",
                "primary-fixed": "#6dfe9c",
                "outline-variant": "#3d4a3e",
                "surface-tint": "#4de082",
                "surface-container-low": "#131b2e",
                "on-primary-fixed-variant": "#005227",
                "on-surface-variant": "#bccabb",
                "secondary-container": "#00cbe6",
                "background": "#0b1326",
                "error": "#ffb4ab",
                "on-error-container": "#ffdad6",
                "on-background": "#dae2fd",
                "secondary-fixed": "#a2eeff",
                "on-surface": "#dae2fd",
                "on-secondary-container": "#00515d",
                "on-error": "#690005",
                "inverse-surface": "#dae2fd",
                "error-container": "#93000a",
                "on-secondary-fixed-variant": "#004e5a",
                "surface-container": "#171f33",
                "inverse-on-surface": "#283044",
                "tertiary": "#ffdd75",
                "surface-bright": "#31394d",
                "primary-container": "#4ade80",
                "primary": "#6bfb9a",
                "tertiary-container": "#ebbf00",
                "on-secondary": "#00363e",
                "tertiary-fixed-dim": "#eec200",
                "outline": "#869486",
                "on-secondary-fixed": "#001f25",
                "on-tertiary-fixed": "#231b00",
                "surface-container-highest": "#2d3449",

                // --- Login -----------------------------------------------
                brand: {
                    darkest: "#0b1320",
                    dark: "#111827",
                    navy: "#1a2235",
                    mint: "#5eead4",
                    mintDark: "#14b8a6",
                    textMuted: "#9ca3af",
                    border: "#374151"
                },

                // --- Registration ----------------------------------------
                "innova-dark": "#111827",       // gray-900
                "innova-card": "#1f2937",       // gray-800
                "innova-mint": "#4ade80",       // green-400
                "innova-mint-hover": "#22c55e"  // green-500
            },
            borderRadius: {
                "DEFAULT": "0.125rem",
                "lg": "0.25rem",
                "xl": "0.5rem",
                "full": "0.75rem"
            },
            spacing: {
                "container-max": "1440px",
                "xl": "64px",
                "sidebar-width": "280px",
                "grid-unit": "4px",
                "md": "24px",
                "lg": "40px",
                "xs": "8px",
                "sm": "16px"
            },
            fontFamily: {
                "body-lg": ["Inter"],
                "headline-lg-mobile": ["Hanken Grotesk"],
                "headline-md": ["Hanken Grotesk"],
                "body-md": ["Inter"],
                "data-mono": ["JetBrains Mono"],
                "label-caps": ["JetBrains Mono"],
                "headline-lg": ["Hanken Grotesk"],
                "display-lg": ["Hanken Grotesk"],
                sans: ["Inter", "ui-sans-serif", "system-ui", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"],
                mono: ["ui-monospace", "SFMono-Regular", "Menlo", "Monaco", "Consolas", "Liberation Mono", "Courier New", "monospace"]
            },
            fontSize: {
                "body-lg": ["18px", { lineHeight: "28px", fontWeight: "400" }],
                "headline-lg-mobile": ["28px", { lineHeight: "34px", fontWeight: "700" }],
                "headline-md": ["24px", { lineHeight: "32px", fontWeight: "600" }],
                "body-md": ["16px", { lineHeight: "24px", fontWeight: "400" }],
                "data-mono": ["14px", { lineHeight: "20px", fontWeight: "500" }],
                "label-caps": ["12px", { lineHeight: "16px", letterSpacing: "0.1em", fontWeight: "700" }],
                "headline-lg": ["32px", { lineHeight: "40px", fontWeight: "700" }],
                "display-lg": ["48px", { lineHeight: "1.1", letterSpacing: "-0.02em", fontWeight: "800" }]
            },
            backgroundImage: {
                "grid-pattern": "linear-gradient(to right, rgba(255,255,255,0.05) 1px, transparent 1px), linear-gradient(to bottom, rgba(255,255,255,0.05) 1px, transparent 1px)"
            }
        }
    }
}
