
        // Profile Dropdown functionality
        const profileButton = document.getElementById('profileButton');
        const dropdownMenu = document.getElementById('dropdownMenu');
        const dropdownIcon = profileButton.querySelector('.dropdown-icon');

        profileButton.addEventListener('click', function (e) {
            e.stopPropagation();
            dropdownMenu.classList.toggle('active');
            dropdownIcon.classList.toggle('rotated');
        });

        document.addEventListener('click', function (e) {
            if (!e.target.closest('.profile-button') && !e.target.closest('.dropdown-menu')) {
                dropdownMenu.classList.remove('active');
                dropdownIcon.classList.remove('rotated');
            }
        });

        // Logout: clear session data before redirecting
        const logoutBtn = document.getElementById('logoutBtn');
        if (logoutBtn) {
            logoutBtn.addEventListener('click', function () {
                localStorage.removeItem('loggedInUsername');
                localStorage.removeItem('loggedInPassword');
            });
        }

        // Initialize notification badge
        window.addEventListener('DOMContentLoaded', function () {
        if (typeof NotificationManager !== 'undefined') {
            NotificationManager.updateBadge();
        }
    });
 