---
name: Aurum Operations
colors:
  surface: '#fbf8ff'
  surface-dim: '#dad9e3'
  surface-bright: '#fbf8ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f4f2fd'
  surface-container: '#eeedf7'
  surface-container-high: '#e8e7f1'
  surface-container-highest: '#e3e1ec'
  on-surface: '#1a1b22'
  on-surface-variant: '#4d4635'
  inverse-surface: '#2f3038'
  inverse-on-surface: '#f1effa'
  outline: '#7f7663'
  outline-variant: '#d0c5af'
  surface-tint: '#735c00'
  primary: '#735c00'
  on-primary: '#ffffff'
  primary-container: '#d4af37'
  on-primary-container: '#554300'
  inverse-primary: '#e9c349'
  secondary: '#5f5e5e'
  on-secondary: '#ffffff'
  secondary-container: '#e2dfde'
  on-secondary-container: '#636262'
  tertiary: '#006c49'
  on-tertiary: '#ffffff'
  tertiary-container: '#33ca90'
  on-tertiary-container: '#005035'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#ffe088'
  primary-fixed-dim: '#e9c349'
  on-primary-fixed: '#241a00'
  on-primary-fixed-variant: '#574500'
  secondary-fixed: '#e5e2e1'
  secondary-fixed-dim: '#c8c6c5'
  on-secondary-fixed: '#1c1b1b'
  on-secondary-fixed-variant: '#474746'
  tertiary-fixed: '#6ffbbe'
  tertiary-fixed-dim: '#4edea3'
  on-tertiary-fixed: '#002113'
  on-tertiary-fixed-variant: '#005236'
  background: '#fbf8ff'
  on-background: '#1a1b22'
  surface-variant: '#e3e1ec'
typography:
  display-lg:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 40px
    fontWeight: '700'
    lineHeight: 52px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 40px
  headline-md:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  title-lg:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-md:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.05em
  data-mono:
    fontFamily: IBM Plex Sans Arabic
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  base: 8px
  xs: 4px
  sm: 12px
  md: 24px
  lg: 40px
  xl: 64px
  container-max: 1440px
  gutter: 24px
---

## Brand & Style

The visual identity of this design system centers on the intersection of high-end luxury and rigorous financial precision. Designed for the gold trading and jewelry management sector, the aesthetic must command trust while reflecting the value of the commodities being managed. 

The style is **Corporate / Modern** with a strong leaning toward **Minimalism**. By using a "less is more" approach, we allow the gold accents to stand out as markers of utility and value rather than mere decoration. The interface is characterized by expansive whitespace, architectural alignment, and a sophisticated light-themed palette that feels airy yet grounded. Every element should feel intentional, high-quality, and tailored for a professional environment where accuracy is paramount.

## Colors

The palette is anchored by **Gold (#D4AF37)**, used strategically for primary actions, active navigation states, and meaningful icons. This is paired with **Deep Black (#1A1A1A)** for typography to ensure maximum legibility and a "premium print" feel. 

- **Backgrounds:** We utilize a triple-layer approach: a base of nearly-white warm gray (#FCFAFA), surface cards in pure white (#FFFFFF), and subtle dividers in a very light beige-gray.
- **Accents:** Growth and positive balances are represented by a rich Emerald Green (#10B981). Alerts and stock shortages use a Soft Red (#EF4444) that is clear without being visually jarring.
- **Neutrals:** Slate and Zinc grays are used for secondary text and decorative borders to maintain a professional, calm atmosphere.

## Typography

This design system utilizes **IBM Plex Sans Arabic** to provide a technical, clean, and highly legible experience across all Arabic text. The hierarchy is designed for data-density:

- **Headlines:** Use Deep Black and semi-bold weights to create a strong visual anchor for page sections.
- **Body Text:** Set in Slate Gray to reduce eye strain during long periods of administrative work.
- **Numerical Data:** For weights (grams), purities (karats), and currency values, use the `data-mono` style. This ensures that columns of numbers align vertically in tables and dashboard widgets.
- **RTL Considerations:** Line heights are slightly increased compared to Latin standards to accommodate the taller ascenders and descenders of the Arabic script, ensuring characters do not touch between lines.

## Layout & Spacing

The layout follows a **Fixed Grid** model for desktop, centered within a 1440px container. This provides a stable environment for complex ERP workflows where the position of data needs to be predictable.

- **Grid:** A 12-column system with 24px gutters.
- **Side Navigation:** A fixed RTL sidebar (280px) provides primary navigation. 
- **Modules:** Content is organized into "Modules" or "Cards." Internal padding for cards is strictly 24px (md) to maintain a consistent breathing room.
- **Density:** While the design is minimal, the spacing is compact enough to allow for viewing 15-20 table rows without scrolling, optimizing for operational efficiency.

## Elevation & Depth

To maintain a premium SaaS feel, we avoid heavy shadows. Instead, we use a combination of **Tonal Layers** and **Ambient Shadows**.

- **Level 0 (Background):** The warm gray foundation.
- **Level 1 (Cards/Sheets):** Pure white surfaces with a very soft, diffused shadow (0px 4px 20px rgba(0,0,0,0.04)). This creates a subtle "lift" from the background.
- **Level 2 (Dropdowns/Modals):** A more defined shadow to indicate temporary interaction (0px 10px 32px rgba(0,0,0,0.08)) with a 1px soft gray border.
- **Active States:** No shadow change; instead, use a 2px Gold right-border (for RTL) to indicate active navigation or focus.

## Shapes

The shape language is **Soft**, utilizing a 4px (0.25rem) base radius. 

This subtle rounding strikes a balance between the sharp, rigid lines of traditional financial software and the modern, approachable nature of contemporary SaaS. It feels architectural and precise. 
- **Buttons and Inputs:** 4px radius.
- **Cards and Modals:** 8px (rounded-lg) for a more prominent, framed appearance.
- **Data Tags/Chips:** Fully rounded (pill-shaped) to distinguish them from interactive buttons.

## Components

### Buttons
- **Primary:** Gold background (#D4AF37) with White text. No gradients.
- **Secondary:** Deep Black background with White text for high-contrast actions (e.g., "Finalize Sale").
- **Ghost:** Transparent background with Gold text, used for less frequent actions.

### Data Tables
Tables are the heart of this design system.
- **Headers:** Sticky headers with a light beige background and Slate Gray uppercase labels.
- **Rows:** Alternating subtle zebra striping or 1px bottom borders. Hover states should trigger a very pale gold tint.
- **Alignment:** Numbers are right-aligned (monospaced), and text labels are right-aligned (RTL).

### Input Fields
- **Design:** Outlined style with a 1px light gray border. 
- **Focus:** The border transitions to Gold on focus with a subtle gold outer glow.
- **Prefix/Suffix:** Gold units (e.g., "جم" or "g") should be pinned to the left of the input in RTL layouts.

### KPI Cards
Displaying the "Live Gold Price" or "Daily Revenue." These cards should feature a large display-font value, a small emerald green trend indicator, and a gold icon in the top-left corner.

### Navigation (RTL)
The sidebar is located on the right. Icons are placed to the right of the text label. Active states are indicated by the Gold primary color and a vertical gold bar on the far right edge of the menu item.