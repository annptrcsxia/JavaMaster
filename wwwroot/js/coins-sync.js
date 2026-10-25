// Shows the player's coins. They now come from the server, not from the browser.
async function loadCoinsDisplay() {
    try {
        const res = await fetch('/api/me');
        if (!res.ok) throw new Error('HTTP ' + res.status);
        const me = await res.json();

        const sidebarCoins = document.getElementById('sidebar-coins');
        if (sidebarCoins) sidebarCoins.textContent = me.coins;

        const userCoins = document.getElementById('user-coins');
        if (userCoins) userCoins.textContent = me.coins;

        return me.coins;
    } catch (e) {
        console.error('Could not load coins', e);
        return null;
    }
}

window.addEventListener('DOMContentLoaded', loadCoinsDisplay);