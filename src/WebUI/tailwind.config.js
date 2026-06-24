/** @type {import('tailwindcss').Config} */
module.exports = {
    darkMode: 'class',
    content: [
        "./Views/**/*.cshtml",
        "./wwwroot/js/**/*.js",
        "./wwwroot/css/**/*.css"
    ],
    theme: {
        extend: {
            "colors": {
                "on-surface": "rgb(26 27 34 / <alpha-value>)",
                "error": "rgb(186 26 26 / <alpha-value>)",
                "secondary": "rgb(95 94 94 / <alpha-value>)",
                "on-tertiary": "rgb(255 255 255 / <alpha-value>)",
                "tertiary-fixed": "rgb(111 251 190 / <alpha-value>)",
                "secondary-fixed-dim": "rgb(200 198 197 / <alpha-value>)",
                "surface-bright": "rgb(251 248 255 / <alpha-value>)",
                "on-error-container": "rgb(147 0 10 / <alpha-value>)",
                "on-background": "rgb(26 27 34 / <alpha-value>)",
                "surface-container": "rgb(238 237 247 / <alpha-value>)",
                "on-secondary-fixed-variant": "rgb(71 71 70 / <alpha-value>)",
                "primary-container": "rgb(212 175 55 / <alpha-value>)",
                "on-primary-fixed": "rgb(36 26 0 / <alpha-value>)",
                "inverse-on-surface": "rgb(241 239 250 / <alpha-value>)",
                "surface-container-lowest": "rgb(255 255 255 / <alpha-value>)",
                "surface-container-highest": "rgb(227 225 236 / <alpha-value>)",
                "tertiary-container": "rgb(51 202 144 / <alpha-value>)",
                "surface-dim": "rgb(218 217 227 / <alpha-value>)",
                "on-secondary": "rgb(255 255 255 / <alpha-value>)",
                "primary": "rgb(115 92 0 / <alpha-value>)",
                "on-primary-container": "rgb(85 67 0 / <alpha-value>)",
                "outline-variant": "rgb(208 197 175 / <alpha-value>)",
                "on-tertiary-container": "rgb(0 80 53 / <alpha-value>)",
                "primary-fixed": "rgb(255 224 136 / <alpha-value>)",
                "on-error": "rgb(255 255 255 / <alpha-value>)",
                "secondary-container": "rgb(226 223 222 / <alpha-value>)",
                "inverse-primary": "rgb(233 195 73 / <alpha-value>)",
                "secondary-fixed": "rgb(229 226 225 / <alpha-value>)",
                "surface": "rgb(251 248 255 / <alpha-value>)",
                "inverse-surface": "rgb(47 48 56 / <alpha-value>)",
                "on-secondary-fixed": "rgb(28 27 27 / <alpha-value>)",
                "error-container": "rgb(255 218 214 / <alpha-value>)",
                "tertiary-fixed-dim": "rgb(78 222 163 / <alpha-value>)",
                "surface-container-high": "rgb(232 231 241 / <alpha-value>)",
                "on-tertiary-fixed-variant": "rgb(0 82 54 / <alpha-value>)",
                "on-surface-variant": "rgb(77 70 53 / <alpha-value>)",
                "surface-tint": "rgb(115 92 0 / <alpha-value>)",
                "on-primary-fixed-variant": "rgb(87 69 0 / <alpha-value>)",
                "on-secondary-container": "rgb(99 98 98 / <alpha-value>)",
                "on-primary": "rgb(255 255 255 / <alpha-value>)",
                "surface-container-low": "rgb(244 242 253 / <alpha-value>)",
                "tertiary": "rgb(0 108 73 / <alpha-value>)",
                "primary-fixed-dim": "rgb(233 195 73 / <alpha-value>)",
                "on-tertiary-fixed": "rgb(0 33 19 / <alpha-value>)",
                "surface-variant": "rgb(227 225 236 / <alpha-value>)",
                "outline": "rgb(127 118 99 / <alpha-value>)",
                "background": "rgb(251 248 255 / <alpha-value>)"
            },
            "borderRadius": {
                "DEFAULT": "0.125rem",
                "lg": "0.25rem",
                "xl": "0.5rem",
                "full": "0.75rem"
            },
            "spacing": {
                "base": "8px",
                "xl": "64px",
                "gutter": "24px",
                "container-max": "1440px",
                "md": "24px",
                "lg": "40px",
                "sm": "12px",
                "xs": "4px"
            },
            "fontFamily": {
                "headline-md": ["IBM Plex Sans Arabic"],
                "headline-lg": ["IBM Plex Sans Arabic"],
                "body-md": ["IBM Plex Sans Arabic"],
                "body-lg": ["IBM Plex Sans Arabic"],
                "display-lg": ["IBM Plex Sans Arabic"],
                "label-md": ["IBM Plex Sans Arabic"],
                "title-lg": ["IBM Plex Sans Arabic"],
                "data-mono": ["IBM Plex Sans Arabic"]
            },
            "fontSize": {
                "headline-md": ["24px", { "lineHeight": "32px", "fontWeight": "600" }],
                "headline-lg": ["32px", { "lineHeight": "40px", "fontWeight": "600" }],
                "body-md": ["14px", { "lineHeight": "20px", "fontWeight": "400" }],
                "body-lg": ["16px", { "lineHeight": "24px", "fontWeight": "400" }],
                "display-lg": ["40px", { "lineHeight": "52px", "letterSpacing": "-0.02em", "fontWeight": "700" }],
                "label-md": ["12px", { "lineHeight": "16px", "letterSpacing": "0.05em", "fontWeight": "500" }],
                "title-lg": ["20px", { "lineHeight": "28px", "fontWeight": "600" }],
                "data-mono": ["14px", { "lineHeight": "20px", "fontWeight": "600" }]
            }
        }
    },
    plugins: [
        require('@tailwindcss/forms'),
        require('@tailwindcss/container-queries')
    ]
};
