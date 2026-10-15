// Universal function to load and display current coins
function loadCoinsDisplay() {
    // Get current coins from localStorage
    const savedCoins = localStorage.getItem('coins');
    
    let currentCoins;
    
    if (savedCoins === null) {
        // New player - give welcome bonus
        currentCoins = 80;
        localStorage.setItem('coins', '80');
        console.log('🎉 Welcome! You received 80 starter coins!');
    } else {
        // Returning player - load their balance
        currentCoins = parseInt(savedCoins);
    }
    
    // Update sidebar display (if it exists on this page)
    const sidebarCoinsElement = document.getElementById('sidebar-coins');
    if (sidebarCoinsElement) {
        sidebarCoinsElement.textContent = currentCoins;
    }
    
    // Update shop page display (if it exists on this page)
    const userCoinsElement = document.getElementById('user-coins');
    if (userCoinsElement) {
        userCoinsElement.textContent = currentCoins;
    }
    
    console.log('💰 Current coins loaded:', currentCoins);
    
    return currentCoins;
}

// Call this function when page loads
window.addEventListener('DOMContentLoaded', loadCoinsDisplay);