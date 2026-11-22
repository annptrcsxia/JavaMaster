/* ============================================================
   Share Deck window (shared by the Lobby and the Deck overview)

   Use:   JMShare.open(deckId, elementToReturnFocusTo)

   It adds its own styles and markup the first time it opens, so a page
   only needs to load this file with a script tag.
   ============================================================ */
(function () {
    'use strict';

    // TODO: read and save the deck through the Decks API once it exists.
    // For now the decks live in this browser tab's sessionStorage (shared with the Lobby and the Deck overview).
    const DECKS_KEY = 'jm.decks';
    const MAX_TYPE_CHIPS = 2;   // more types than this show as "+3"
    const COPIED_MS = 2000;     // how long "Link copied!" stays on the button
    const TYPE_LABELS = {
        'multiple-choice': 'Multiple Choice',
        'true-false': 'True or False',
        'matching': 'Matching Type',
        'fill-blank-code': 'Fill in the Blanks Code',
        'drag-drop-code': 'Drag and Drop Code'
    };

    const GLOBE = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c2.5 2.7 3.8 5.7 3.8 9s-1.3 6.3-3.8 9c-2.5-2.7-3.8-5.7-3.8-9S9.5 5.7 12 3z"/></svg>';
    const LOCK = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="5" y="11" width="14" height="9" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/></svg>';
    const ICONS = { 'public': GLOBE, 'private': LOCK };
    const LABELS = { 'public': 'Public', 'private': 'Private' };

    const CSS = `
.jms-overlay { position: fixed; inset: 0; z-index: 1000; display: flex; align-items: center; justify-content: center; padding: 16px; background: rgba(0, 0, 0, .65); }
.jms-overlay[hidden] { display: none; }
.jms-dialog { position: relative; width: min(480px, 100%); padding: 34px 28px 28px; border-radius: 28px; background: #332b27; color: #f3e6d8; font-family: 'Bricolage Grotesque', sans-serif; box-shadow: 0 20px 50px rgba(0, 0, 0, .55); animation: jmsIn .25s cubic-bezier(.22, 1, .36, 1) both; }
.jms-x { position: absolute; top: 18px; right: 22px; border: none; background: none; color: #f3e6d8; font-size: 30px; font-weight: 300; line-height: 1; cursor: pointer; }
.jms-dialog h2 { margin: 0 0 22px; text-align: center; font-size: 30px; font-weight: 500; }

.jms-deck { display: flex; overflow: hidden; border: 1px solid #7a5530; border-radius: 22px; background: #2b2320; }
.jms-cover { flex: none; width: 124px; object-fit: cover; }
.jms-deck-info { min-width: 0; padding: 18px 20px; }
.jms-name { margin: 0 0 10px; font-size: 24px; font-weight: 600; overflow-wrap: anywhere; }
.jms-tags { display: flex; flex-wrap: wrap; gap: 6px; }
.jms-chip { padding: 4px 12px; border-radius: 14px; background: #6b3414; font-size: 12px; font-weight: 500; }

.jms-row { display: flex; align-items: center; gap: 14px; margin-top: 22px; }
.jms-label { flex: none; font-size: 18px; font-weight: 600; }
.jms-select { position: relative; flex: 1; min-width: 0; }
.jms-select-btn { display: flex; align-items: center; gap: 12px; width: 100%; height: 44px; padding: 0 16px; border: 2px solid #8f5f2a; border-radius: 14px; background: transparent; color: #f3e6d8; font: 500 16px 'Bricolage Grotesque', sans-serif; cursor: pointer; transition: border-color .2s, box-shadow .2s; }
.jms-select-btn:hover { border-color: #b57a34; }
.jms-select-btn[aria-expanded="true"] { border-color: #e0700f; box-shadow: 0 0 0 3px rgba(224, 112, 15, .22); }
.jms-sel-ico, .jms-ico { display: flex; flex: none; width: 22px; height: 22px; }
.jms-sel-ico { width: 20px; height: 20px; color: #e0700f; }
.jms-sel-ico svg, .jms-ico svg { width: 100%; height: 100%; }
.jms-caret { width: 8px; height: 8px; margin-left: auto; border-right: 2px solid #f3e6d8; border-bottom: 2px solid #f3e6d8; transform: rotate(45deg) translateY(-2px); transition: transform .2s; }
.jms-select-btn[aria-expanded="true"] .jms-caret { transform: rotate(-135deg) translateY(-2px); }

.jms-options { position: absolute; top: calc(100% + 8px); left: 0; right: 0; z-index: 5; margin: 0; padding: 8px; list-style: none; border: 1px solid #5a4339; border-radius: 18px; background: #35261f; box-shadow: 0 18px 40px rgba(0, 0, 0, .55); transform-origin: top center; animation: jmsDrop .2s cubic-bezier(.2, .8, .2, 1) both; }
.jms-options[hidden] { display: none; }
.jms-options li { animation: jmsItem .24s ease-out both; }
.jms-options li:nth-child(2) { animation-delay: .05s; }
.jms-options button { display: flex; align-items: flex-start; gap: 14px; width: 100%; padding: 12px 14px; border: none; border-radius: 12px; background: none; color: #f3e6d8; text-align: left; cursor: pointer; transition: background .15s; }
.jms-options button:hover, .jms-options button:focus-visible { background: #4a352c; outline: none; }
.jms-ico { margin-top: 2px; color: #b9aea5; transition: color .15s; }
.jms-options button:hover .jms-ico, .jms-options [aria-selected="true"] .jms-ico { color: #e0700f; }
.jms-opt-text { display: flex; flex: 1; flex-direction: column; gap: 2px; }
.jms-opt-text strong { font: 600 16px 'Bricolage Grotesque', sans-serif; }
.jms-opt-text small { font: 400 13px/1.3 'Bricolage Grotesque', sans-serif; color: #b9aea5; }
.jms-check { flex: none; width: 18px; height: 18px; margin-top: 2px; opacity: 0; }
.jms-check::after { content: ''; display: block; width: 5px; height: 10px; margin: 1px 0 0 6px; border: solid #e0700f; border-width: 0 2px 2px 0; transform: rotate(45deg); }
.jms-options [aria-selected="true"] .jms-check { opacity: 1; }

.jms-hint { min-height: 18px; margin: 14px 0 0; font-size: 13px; color: #b9aea5; text-align: center; }
.jms-copy { display: block; width: 100%; margin-top: 8px; padding: 14px 0; border: none; border-radius: 30px; background: #c8662d; color: #fff; font: 500 22px 'Bricolage Grotesque', sans-serif; cursor: pointer; transition: background .15s; }
.jms-copy:hover:not(:disabled) { background: #d8733a; }
.jms-copy:disabled { background: #6e5448; color: #b9a99f; cursor: not-allowed; }
.jms-dialog :focus-visible { outline: 3px solid #4a9eff; outline-offset: 2px; }
.jms-status { position: absolute; width: 1px; height: 1px; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); }

@keyframes jmsIn { from { opacity: 0; transform: translateY(-16px); } to { opacity: 1; transform: none; } }
@keyframes jmsDrop { from { opacity: 0; transform: translateY(-8px) scale(.97); } to { opacity: 1; transform: none; } }
@keyframes jmsItem { from { opacity: 0; transform: translateY(-6px); } to { opacity: 1; transform: none; } }
@media (prefers-reduced-motion: reduce) { .jms-dialog, .jms-options, .jms-options li { animation: none; } }
@media (max-width: 480px) { .jms-row { flex-direction: column; align-items: stretch; gap: 8px; } .jms-cover { width: 96px; } }
`;

    const HTML = `
<div class="jms-dialog" role="dialog" aria-modal="true" aria-labelledby="jmsTitle">
    <button type="button" class="jms-x" aria-label="Close">×</button>
    <h2 id="jmsTitle">Share Deck</h2>

    <div class="jms-deck">
        <img class="jms-cover" alt="">
        <div class="jms-deck-info">
            <h3 class="jms-name"></h3>
            <div class="jms-tags"></div>
        </div>
    </div>

    <div class="jms-row">
        <span class="jms-label" id="jmsLabel">Share with:</span>
        <div class="jms-select">
            <button type="button" class="jms-select-btn" aria-haspopup="listbox" aria-expanded="false" aria-labelledby="jmsLabel jmsSelText">
                <span class="jms-sel-ico" aria-hidden="true"></span>
                <span id="jmsSelText"></span>
                <span class="jms-caret" aria-hidden="true"></span>
            </button>
            <ul class="jms-options" role="listbox" aria-labelledby="jmsLabel" hidden>
                <li role="presentation">
                    <button type="button" role="option" aria-selected="false" data-value="public">
                        <span class="jms-ico" aria-hidden="true">${GLOBE}</span>
                        <span class="jms-opt-text"><strong>Public</strong><small>Anyone can find and play this deck</small></span>
                        <span class="jms-check" aria-hidden="true"></span>
                    </button>
                </li>
                <li role="presentation">
                    <button type="button" role="option" aria-selected="false" data-value="private">
                        <span class="jms-ico" aria-hidden="true">${LOCK}</span>
                        <span class="jms-opt-text"><strong>Private</strong><small>Only you can see this deck</small></span>
                        <span class="jms-check" aria-hidden="true"></span>
                    </button>
                </li>
            </ul>
        </div>
    </div>

    <p class="jms-hint"></p>
    <button type="button" class="jms-copy">Copy link</button>
    <p class="jms-status" role="status"></p>
</div>`;

    let root = null;       // the dark overlay that holds the window
    let ui = null;         // the parts of the window we update
    let deck = null;       // the deck being shared
    let opener = null;     // where focus goes back to when the window closes
    let copyTimer = null;

    function readList() {
        try {
            const list = JSON.parse(sessionStorage.getItem(DECKS_KEY));
            return Array.isArray(list) ? list : [];
        } catch (e) {
            return [];
        }
    }

    function el(tag, cls, text) {
        const node = document.createElement(tag);
        if (cls) node.className = cls;
        if (text !== undefined) node.textContent = text;   // text only, so a deck name can never run as code
        return node;
    }

    // TODO: point this at the page other players open to play a shared deck, once that page exists.
    function deckLink() {
        return new URL('/pages/Deck overview.html?id=' + encodeURIComponent(deck.id), location.href).href;
    }

    async function copyText(text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (e) { /* fall through to the older way */ }
        const box = document.createElement('textarea');
        box.value = text;
        box.setAttribute('readonly', '');
        box.style.cssText = 'position:fixed;opacity:0';
        document.body.appendChild(box);
        box.select();
        let ok = false;
        try { ok = document.execCommand('copy'); } catch (e) { ok = false; }
        box.remove();
        return ok;
    }

    function build() {
        const style = document.createElement('style');
        style.id = 'jmsStyle';
        style.textContent = CSS;
        document.head.appendChild(style);

        root = el('div', 'jms-overlay');
        root.hidden = true;
        root.innerHTML = HTML;
        document.body.appendChild(root);

        const q = s => root.querySelector(s);
        ui = {
            dialog: q('.jms-dialog'),
            close: q('.jms-x'),
            cover: q('.jms-cover'),
            name: q('.jms-name'),
            tags: q('.jms-tags'),
            selectBox: q('.jms-select'),
            selectBtn: q('.jms-select-btn'),
            selIcon: q('.jms-sel-ico'),
            selText: q('#jmsSelText'),
            list: q('.jms-options'),
            options: [...root.querySelectorAll('.jms-options button')],
            hint: q('.jms-hint'),
            copy: q('.jms-copy'),
            status: q('.jms-status')
        };

        ui.close.addEventListener('click', close);
        root.addEventListener('click', e => { if (e.target === root) close(); });
        ui.dialog.addEventListener('click', e => { if (!ui.selectBox.contains(e.target)) toggleList(false); });
        ui.selectBtn.addEventListener('click', () => toggleList(ui.list.hidden));
        ui.options.forEach(b => b.addEventListener('click', () => {
            setShare(b.dataset.value, true);
            toggleList(false);
            ui.selectBtn.focus();
        }));
        ui.copy.addEventListener('click', onCopy);
    }

    function toggleList(open) {
        ui.list.hidden = !open;
        ui.selectBtn.setAttribute('aria-expanded', String(open));
    }

    // Updates the window for Public or Private. With save = true the choice is also kept on the deck.
    function setShare(value, save) {
        ui.selText.textContent = LABELS[value];
        ui.selIcon.innerHTML = ICONS[value];
        ui.options.forEach(b => b.setAttribute('aria-selected', String(b.dataset.value === value)));

        const isPublic = value === 'public';
        ui.copy.disabled = !isPublic;
        ui.hint.textContent = isPublic ? '' : 'Only you can see this deck. Switch to Public to copy a link.';
        clearTimeout(copyTimer);
        ui.copy.textContent = 'Copy link';

        deck.share = value;
        if (save) {
            try {
                const list = readList();
                const saved = list.find(d => String(d.id) === String(deck.id));
                if (saved) {
                    saved.share = value;
                    sessionStorage.setItem(DECKS_KEY, JSON.stringify(list));
                }
            } catch (e) { /* the window still works if the browser blocks storage */ }
        }
    }

    async function onCopy() {
        if (ui.copy.disabled) return;
        const ok = await copyText(deckLink());
        clearTimeout(copyTimer);
        ui.copy.textContent = ok ? 'Link copied!' : 'Could not copy';
        ui.status.textContent = ui.copy.textContent;
        copyTimer = setTimeout(() => { ui.copy.textContent = 'Copy link'; ui.status.textContent = ''; }, COPIED_MS);
    }

    function fill() {
        ui.cover.src = '/cover/cover ' + deck.cover + '.jpg';
        ui.name.textContent = deck.name;

        ui.tags.innerHTML = '';
        const n = deck.cards.length;
        ui.tags.appendChild(el('span', 'jms-chip', n + (n === 1 ? ' Card' : ' Cards')));
        const types = deck.types || [];
        types.slice(0, MAX_TYPE_CHIPS).forEach(t => ui.tags.appendChild(el('span', 'jms-chip', TYPE_LABELS[t] || t)));
        if (types.length > MAX_TYPE_CHIPS) ui.tags.appendChild(el('span', 'jms-chip', '+' + (types.length - MAX_TYPE_CHIPS)));

        toggleList(false);
        ui.status.textContent = '';
        setShare(deck.share === 'private' ? 'private' : 'public', false);
    }

    function onKey(e) {
        if (e.key === 'Escape') {
            e.preventDefault();
            if (!ui.list.hidden) { toggleList(false); ui.selectBtn.focus(); } else close();
            return;
        }
        if (e.key === 'Tab') {   // keep Tab inside the window
            const items = [...ui.dialog.querySelectorAll('button:not([disabled])')].filter(b => !b.closest('[hidden]'));
            if (!items.length) return;
            const first = items[0], last = items[items.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
            return;
        }
        if ((e.key === 'ArrowDown' || e.key === 'ArrowUp') && (document.activeElement === ui.selectBtn || ui.list.contains(document.activeElement))) {
            e.preventDefault();
            toggleList(true);
            const i = ui.options.indexOf(document.activeElement);
            ui.options[(i + (e.key === 'ArrowDown' ? 1 : -1) + ui.options.length) % ui.options.length].focus();
        }
    }

    function open(id, from) {
        const found = readList().find(d => String(d.id) === String(id));
        if (!found || !Array.isArray(found.cards)) return false;
        if (!root) build();
        deck = found;
        opener = from || document.activeElement;
        fill();
        root.hidden = false;
        document.addEventListener('keydown', onKey);
        ui.close.focus();
        return true;
    }

    function close() {
        if (!root || root.hidden) return;
        root.hidden = true;
        clearTimeout(copyTimer);
        document.removeEventListener('keydown', onKey);
        if (opener && typeof opener.focus === 'function') opener.focus();
    }

    window.JMShare = { open: open, close: close };
})();