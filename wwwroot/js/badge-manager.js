/* ============================================================
   badge-manager.js
   - Profile page: fills #badgeCount from localStorage 'badges'
   - Learn page:   shows the "+1 Badge" toast after a lesson was
                   completed (flag 'pendingBadge' set by the lesson)
   Include with: <script src="/js/badge-manager.js"></script>
   ============================================================ */
(function () {
    'use strict';

    var BADGE_ICON = '/icons/badge.png';

    function read(key) {
        try { return localStorage.getItem(key); } catch (e) { return null; }
    }

    /* ---------- Profile: badge count ---------- */
    function updateBadgeCount() {
        var el = document.getElementById('badgeCount');
        if (el) el.textContent = parseInt(read('badges'), 10) || 0;
    }

    /* ---------- Learn: "+1 Badge" toast ---------- */
    function injectStyles() {
        if (document.getElementById('badgeToastStyles')) return;
        var css = [
            '.badge-toast {',
            '    position: fixed;',
            '    right: 5vw;',
            '    top: 68vh;',
            '    z-index: 200;',
            '    display: flex;',
            '    align-items: center;',
            '    gap: 14px;',
            '    width: 300px;',
            '    max-width: 90vw;',
            '    padding: 16px 18px;',
            '    box-sizing: border-box;',
            '    background: #05365a;',
            '    border: 1.5px solid #1b73b8;',
            '    border-radius: 12px;',
            '    box-shadow: 0 10px 28px rgba(0, 0, 0, .45);',
            '    color: #e6f1fb;',
            '    font-family: "Inter", "Bricolage Grotesque", sans-serif;',
            '    opacity: 0;',
            '    transform: translateY(14px);',
            '    transition: opacity .4s ease, transform .4s ease;',
            '}',
            '.badge-toast.show { opacity: 1; transform: translateY(0); }',
            '.badge-toast img { width: 34px; height: auto; flex-shrink: 0; }',
            '.badge-toast .bt-title { font-size: 14px; font-weight: 600; color: #fff; margin-bottom: 6px; }',
            '.badge-toast .bt-text { font-size: 13px; line-height: 1.4; color: #b9d3ea; }',
            '.badge-toast .bt-close {',
            '    position: absolute;',
            '    top: 8px;',
            '    right: 12px;',
            '    background: none;',
            '    border: none;',
            '    color: #8fb5d6;',
            '    font-size: 18px;',
            '    line-height: 1;',
            '    cursor: pointer;',
            '}',
            '@media (prefers-reduced-motion: reduce) { .badge-toast { transition: none; } }'
        ].join('\n');
        var style = document.createElement('style');
        style.id = 'badgeToastStyles';
        style.textContent = css;
        document.head.appendChild(style);
    }

    function showBadgeToast() {
        injectStyles();
        var toast = document.createElement('div');
        toast.className = 'badge-toast';
        toast.setAttribute('role', 'status');
        toast.innerHTML =
            '<img src="' + BADGE_ICON + '" alt="badge">' +
            '<div>' +
            '  <div class="bt-title">+1 Badge</div>' +
            '  <div class="bt-text">You earned a badge for completing this lesson. Keep it up!</div>' +
            '</div>' +
            '<button class="bt-close" aria-label="Close">&times;</button>';
        document.body.appendChild(toast);

        function close() {
            toast.classList.remove('show');
            setTimeout(function () { toast.remove(); }, 450);
        }
        toast.querySelector('.bt-close').addEventListener('click', close);
        requestAnimationFrame(function () { toast.classList.add('show'); });
        setTimeout(close, 6000);
    }

    function checkPendingBadge() {
        if (!document.getElementById('lessonPath')) return;   // Learn page only
        if (!read('pendingBadge')) return;
        try { localStorage.removeItem('pendingBadge'); } catch (e) {}
        setTimeout(showBadgeToast, 600);
    }

    /* ---------- Init ---------- */
    window.addEventListener('load', function () {
        updateBadgeCount();
        checkPendingBadge();
    });

    window.addEventListener('storage', function (e) {
        if (e.key === 'badges') updateBadgeCount();
    });
})();