// notificationManager.js - Central Notification System for Java Master
// Include this file in all your HTML pages: <script src="notificationManager.js"></script>

const NotificationManager = {
    /**
     * Add a new notification
     * @param {string} message - The notification message
     * @param {string} icon - Emoji icon for the notification
     * @param {string} type - Type of notification (streak, challenge, coins, hint, hearts, shop, etc.)
     * @returns {object} The created notification object
     */
    add(message, icon, type) {
        const notifications = this.getAll();
        const newNotification = {
            id: Date.now(),
            message: message,
            icon: icon,
            type: type,
            timestamp: new Date().toISOString(),
            read: false
        };
        notifications.unshift(newNotification);
        localStorage.setItem('notifications', JSON.stringify(notifications));
        this.updateBadge();
        return newNotification;
    },

    /**
     * Get all notifications
     * @returns {array} Array of notification objects
     */
    getAll() {
        const stored = localStorage.getItem('notifications');
        return stored ? JSON.parse(stored) : [];
    },

    /**
     * Mark all notifications as read
     */
    markAllRead() {
        const notifications = this.getAll();
        notifications.forEach(n => n.read = true);
        localStorage.setItem('notifications', JSON.stringify(notifications));
        this.updateBadge();
    },

    /**
     * Clear all notifications
     */
    clearAll() {
        localStorage.setItem('notifications', JSON.stringify([]));
        this.updateBadge();
    },

    /**
     * Update the notification badge visibility
     */
    updateBadge() {
        const notifications = this.getAll();
        const hasUnread = notifications.some(n => !n.read);
        localStorage.setItem('hasUnreadNotifications', hasUnread);
        
        // Update badge on current page if it exists
        const badge = document.getElementById('notificationBadge');
        if (badge) {
            if (hasUnread) {
                badge.classList.remove('hidden');
            } else {
                badge.classList.add('hidden');
            }
        }
    },

    /**
     * Get the count of unread notifications
     * @returns {number} Number of unread notifications
     */
    getUnreadCount() {
        return this.getAll().filter(n => !n.read).length;
    },

    /**
     * Format timestamp to human-readable time
     * @param {string} timestamp - ISO timestamp string
     * @returns {string} Formatted time string
     */
    formatTime(timestamp) {
        const now = new Date();
        const notifTime = new Date(timestamp);
        const diffMs = now - notifTime;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
        if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
        if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
        return notifTime.toLocaleDateString();
    },

    /**
     * Initialize with default notifications (only if empty)
     */
    initializeDefaults() {
        const existing = this.getAll();
        if (existing.length === 0) {
            this.add("Day 1 streak! You're brewing up some serious Java skills.", "☕", "streak");
            this.add("You completed today's challenge! +10 XP added to your level.", "🎉", "challenge");
            this.add("You earned 30 coins from today's streak. Spend them wisely!", "💰", "coins");
            this.add("Hint used! Don't forget to buy more in the shop.", "💡", "hint");
            this.add("Need to refill more hearts? Buy more in the shop.", "❤️", "hearts");
        }
    }
};

// ===== USAGE EXAMPLES =====

/*

HOW TO USE THIS IN YOUR PAGES:

1. Include this file in your HTML:
   <script src="notificationManager.js"></script>

2. Then use NotificationManager methods anywhere in your page:

// SHOP PAGE - When user purchases something:
NotificationManager.add(
    "You purchased 5 hints! Use them wisely.",
    "💡",
    "shop"
);

NotificationManager.add(
    "You purchased 3 hearts! Keep learning!",
    "❤️",
    "shop"
);

// PROGRESS PAGE - When user completes a streak:
NotificationManager.add(
    "Day 3 streak! You're brewing up some serious Java skills.",
    "☕",
    "streak"
);

NotificationManager.add(
    "You earned 50 coins from today's streak. Spend them wisely!",
    "💰",
    "coins"
);

// DAILY CHALLENGES PAGE - When challenge completed:
NotificationManager.add(
    "You completed today's challenge! +10 XP added to your level.",
    "🎉",
    "challenge"
);

// LEARN PAGE - When hearts are low:
if (hearts <= 1) {
    NotificationManager.add(
        "Need to refill more hearts? Buy more in the shop.",
        "❤️",
        "hearts"
    );
}

// LEARN PAGE - When hint is used:
NotificationManager.add(
    "Hint used! Don't forget to buy more in the shop.",
    "💡",
    "hint"
);

// MY LEVEL PAGE - When user levels up:
NotificationManager.add(
    "Congratulations! You reached level 5! 🎊",
    "⭐",
    "levelup"
);

// Any achievement:
NotificationManager.add(
    "Achievement unlocked: Java Champion! 🏆",
    "🏆",
    "achievement"
);

// Check unread count:
const unreadCount = NotificationManager.getUnreadCount();
console.log(`You have ${unreadCount} unread notifications`);

// Get all notifications:
const allNotifs = NotificationManager.getAll();
console.log(allNotifs);

*/