/* ═══════════════════════════════════════════════════════
   GOLD PRICES — Shared KPI loader with localStorage caching
   Used on: Login page, Inventory page
   Cache TTL: 1 day (86400000 ms)
   ═══════════════════════════════════════════════════════ */

const GOLD_PRICES_CACHE_KEY = 'goldStore_kpis';
const GOLD_PRICES_CACHE_TTL = 86400000; // 1 day in ms

async function loadGoldPricesKpis() {
    const cached = readGoldPricesCache();
    if (cached) {
        return cached;
    }

    try {
        const res = await fetch('/Inventory/GetKpis');
        const data = await res.json();
        if (!data || data.success === false) return null;

        writeGoldPricesCache(data);
        return data;
    } catch (e) {
        console.error('Failed to load gold prices KPIs', e);
        return null;
    }
}

function readGoldPricesCache() {
    try {
        const raw = localStorage.getItem(GOLD_PRICES_CACHE_KEY);
        if (!raw) return null;

        const parsed = JSON.parse(raw);
        if (!parsed || !parsed.timestamp || !parsed.data) return null;

        const age = Date.now() - parsed.timestamp;
        if (age > GOLD_PRICES_CACHE_TTL) {
            localStorage.removeItem(GOLD_PRICES_CACHE_KEY);
            return null;
        }

        return parsed.data;
    } catch (e) {
        return null;
    }
}

function writeGoldPricesCache(data) {
    try {
        const payload = JSON.stringify({ data: data, timestamp: Date.now() });
        localStorage.setItem(GOLD_PRICES_CACHE_KEY, payload);
    } catch (e) {
        console.error('Failed to cache gold prices', e);
    }
}

function clearGoldPricesCache() {
    localStorage.removeItem(GOLD_PRICES_CACHE_KEY);
}

function renderGoldPriceChange(change, direction) {
    if (direction === 'up') {
        return '<div class="mt-2 flex items-center gap-1 text-tertiary"><span class="material-symbols-outlined text-[16px]">arrow_upward</span><span class="font-data-mono text-data-mono text-sm">+' + (change || 0).toFixed(2) + '%</span></div>';
    } else if (direction === 'down') {
        return '<div class="mt-2 flex items-center gap-1 text-error"><span class="material-symbols-outlined text-[16px]">arrow_downward</span><span class="font-data-mono text-data-mono text-sm">' + (change || 0).toFixed(2) + '%</span></div>';
    }
    return '<div class="mt-2 flex items-center gap-1 text-secondary"><span class="font-data-mono text-data-mono text-sm">—</span></div>';
}
