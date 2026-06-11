import $ from "jquery";
import '../css/style.css';

function applyTheme(theme: string, color: string) {
    const html = document.documentElement;
    if (theme === 'system') {
        html.removeAttribute('data-theme');
    } else {
        html.setAttribute('data-theme', theme);
    }
    html.setAttribute('data-color', color);
}

(window as any).dmftTheme = { applyTheme };

const metaTheme = document.querySelector('meta[name="dmft-theme"]')?.getAttribute('content') || 'system';
const metaColor = document.querySelector('meta[name="dmft-color"]')?.getAttribute('content') || 'blue';
applyTheme(metaTheme, metaColor);

export { $ };
