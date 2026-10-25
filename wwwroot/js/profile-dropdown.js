// Profile Dropdown functionality
(function () {
    const profileButton = document.getElementById('profileButton');
    const dropdownMenu = document.getElementById('dropdownMenu');
    if (!profileButton || !dropdownMenu) return;   // page without the top bar

    const dropdownIcon = profileButton.querySelector('.dropdown-icon');

    profileButton.setAttribute('aria-haspopup', 'true');
    profileButton.setAttribute('aria-expanded', 'false');

    function setOpen(open) {
        dropdownMenu.classList.toggle('active', open);
        if (dropdownIcon) dropdownIcon.classList.toggle('rotated', open);
        profileButton.setAttribute('aria-expanded', String(open));
    }

    profileButton.addEventListener('click', function (e) {
        e.stopPropagation();
        setOpen(!dropdownMenu.classList.contains('active'));
    });

    // Close when clicking anywhere outside the button/menu
    document.addEventListener('click', function (e) {
        if (!e.target.closest('.profile-button') && !e.target.closest('.dropdown-menu')) {
            setOpen(false);
        }
    });

    // Close with the Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && dropdownMenu.classList.contains('active')) {
            setOpen(false);
            profileButton.focus();
        }
    });

    // Close after choosing an item (helps when the page doesn't fully reload)
    dropdownMenu.querySelectorAll('.dropdown-item').forEach(function (item) {
        item.addEventListener('click', function () { setOpen(false); });
    });

    // Close when coming back with the Back button (page restored from cache)
    window.addEventListener('pageshow', function (e) {
        if (e.persisted) setOpen(false);
    });

    // Logout: clear session data before redirecting
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', function () {
            localStorage.removeItem('loggedInUsername');
            localStorage.removeItem('loggedInPassword');
            // If api.js stores a token/session, clear it here too
        });
    }

    // Initialize notification badge
    window.addEventListener('DOMContentLoaded', function () {
        if (typeof NotificationManager !== 'undefined') {
            NotificationManager.updateBadge();
        }
    });
})();