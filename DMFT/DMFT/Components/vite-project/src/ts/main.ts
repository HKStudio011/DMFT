import $ from "jquery";
import '../css/style.css';

let currentThemeSetting = 'system';

function applyTheme(theme: string, color: string) {
    currentThemeSetting = theme;
    const html = document.documentElement;
    const resolvedTheme = theme === 'system'
        ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
        : theme;
    html.setAttribute('data-theme', resolvedTheme);
    html.setAttribute('data-color', color);
}

(window as any).dmftTheme = { applyTheme };

const metaTheme = document.querySelector('meta[name="dmft-theme"]')?.getAttribute('content') || 'system';
const metaColor = document.querySelector('meta[name="dmft-color"]')?.getAttribute('content') || 'blue';
applyTheme(metaTheme, metaColor);

window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    if (currentThemeSetting === 'system') {
        document.documentElement.setAttribute('data-theme', e.matches ? 'dark' : 'light');
    }
});

export { $ };
