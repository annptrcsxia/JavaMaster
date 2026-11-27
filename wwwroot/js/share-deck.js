/* ============================================================
   Share Deck window (shared by the Lobby and the Deck overview)

   Use:   JMShare.open(deckId, elementToReturnFocusTo)

   Needs /js/api.js loaded first (JM.deck, JM.setDeckShare).
   Window 1: Share Deck (Public / Private + "Generate a code").
   Window 2: Deck Code (the code + "Copy Code").
   ============================================================ */
(function () {
    'use strict';

    const MAX_TYPE_CHIPS = 2;   // more types than this show as "+3"
    const COPIED_MS = 2000;     // how long "Code copied!" stays on the button
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
.jms-dialog[hidden] { display: none; }
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
.jms-select-btn { display: flex; align-items: center; gap: 12px; width: 100%; height: 44px; padding: 0 16px; border: 1px solid #7a5530; border-radius: 14px; background: transparent; color: #f3e6d8; font: 500 16px 'Bricolage Grotesque', sans-serif; cursor: pointer; transition: border-color .2s, box-shadow .2s; }
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

.jms-hint { min-height: 0px; margin: 14px 0 0; font-size: 13px; color: #b9aea5; text-align: center; }
.jms-copy { display: block; width: 100%; margin-top: 8px; padding: 14px 0; border: none; border-radius: 30px; background: #c8662d; color: #fff; font: 500 22px 'Bricolage Grotesque', sans-serif; cursor: pointer; transition: background .15s; }
.jms-copy:hover:not(:disabled) { background: #d8733a; }
.jms-copy:disabled { background: #6e5448; color: #b9a99f; cursor: not-allowed; }
.jms-dialog :focus-visible { outline: 3px solid #4a9eff; outline-offset: 2px; }
.jms-status { position: absolute; width: 1px; height: 1px; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); }

.jms-sub { margin: -10px 0 0; text-align: center; font-size: 15px; color: #d9cdc2; }
.jms-code { margin: 30px 0 34px; text-align: center; font-size: 46px; font-weight: 600; letter-spacing: .12em; color: #f3e6d8; overflow-wrap: anywhere; }

.jms-pub-list { display: flex; flex-direction: column; gap: 12px; max-height: 160px; margin-top: 22px; padding: 2px; overflow-y: auto; }
.jms-pub-item { display: flex; flex: none; overflow: hidden; width: 100%; padding: 0; border: 1px solid #7a5530; border-radius: 22px; background: #2b2320; color: #f3e6d8; text-align: left; cursor: pointer; transition: border-color .15s, box-shadow .15s; }
.jms-pub-item:hover { border-color: #b57a34; }
.jms-pub-item[aria-checked="true"] { border-color: #e0700f; box-shadow: 0 0 0 2px rgba(224, 112, 15, .35); }
.jms-pub-item .jms-cover { width: 92px; }
.jms-pub-item .jms-deck-info { padding: 14px 18px; }
.jms-pub-item .jms-name { margin-bottom: 8px; font-size: 20px; }
.jms-pub-empty { margin: 0; padding: 10px 0; text-align: center; color: #b9aea5; }
.jms-pub-empty a { color: #e0700f; }
.jms-pub-sub[hidden] + .jms-pub-list { margin-top: 0; }
.jms-pub-msg { min-height: 18px; margin: 12px 0 0; font-size: 13px; color: #f0b429; text-align: center; }

.jms-join-dialog .jms-sub { max-width: 330px; margin: -10px auto 0; font-size: 17px; line-height: 1.3; }
.jms-join-boxes { display: flex; justify-content: center; gap: 8px; margin: 28px 0 14px; }
.jms-join-dialog .jms-join-box:focus,
.jms-join-dialog .jms-join-box:focus-visible { outline: none; background: #fff8ee; border-color: #e0632f; box-shadow: 0 6px 0 #c28d6b, 0 0 0 3px rgba(224, 99, 47, .35); }
.jms-join-box { box-sizing: border-box; width: 54px; height: 74px; padding: 0; border: 3px solid transparent; outline: none; border-radius: 14px; background: #efe0d0; box-shadow: 0 6px 0 #c28d6b; color: #3b2a24; text-align: center; font: 600 34px 'Bricolage Grotesque', sans-serif; text-transform: uppercase; caret-color: #c8662d; }

.jms-join-loading { display: none; flex-direction: column; align-items: center; justify-content: center; gap: 18px; min-height: 216px; color: #d9cdc2; font-size: 16px; }
.jms-join-dialog.is-loading .jms-join-loading { display: flex; }
.jms-join-dialog.is-loading .jms-sub,
.jms-join-dialog.is-failed .jms-sub,
.jms-join-dialog.is-failed .jms-join-boxes,
.jms-join-dialog.is-failed .jms-join-go { display: none; }
.jms-join-fail { display: none; flex-direction: column; min-height: 216px; }
.jms-join-dialog.is-failed .jms-join-fail { display: flex; }
.jms-join-msg { display: flex; flex: 1; align-items: center; justify-content: center; margin: 0; text-align: center; font-size: 17px; color: #d9cdc2; }
.jms-join-fail .jms-copy { margin-top: 0; }
.jms-join-dialog.is-loading .jms-join-boxes,
.jms-join-dialog.is-loading .jms-join-go { display: none; }
.jms-spinner { width: 46px; height: 46px; border: 5px solid #5a4339; border-top-color: #e0700f; border-radius: 50%; animation: jmsSpin .8s linear infinite; }
@keyframes jmsSpin { to { transform: rotate(360deg); } }

@keyframes jmsIn { from { opacity: 0; transform: translateY(-16px); } to { opacity: 1; transform: none; } }
@keyframes jmsDrop { from { opacity: 0; transform: translateY(-8px) scale(.97); } to { opacity: 1; transform: none; } }
@keyframes jmsItem { from { opacity: 0; transform: translateY(-6px); } to { opacity: 1; transform: none; } }
@media (prefers-reduced-motion: reduce) { .jms-dialog, .jms-options, .jms-options li { animation: none; } }
@media (max-width: 480px) { .jms-row { flex-direction: column; align-items: stretch; gap: 8px; } .jms-cover { width: 96px; } .jms-code { font-size: 34px; } }
`;

    const HTML = `
<div class="jms-dialog jms-share-dialog" role="dialog" aria-modal="true" aria-labelledby="jmsTitle">
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
    <button type="button" class="jms-copy jms-generate">Generate a code</button>
</div>

<div class="jms-dialog jms-join-dialog" role="dialog" aria-modal="true" aria-labelledby="jmsJoinTitle" hidden>
    <button type="button" class="jms-x" aria-label="Close">×</button>
    <h2 id="jmsJoinTitle">Play a Shared Deck</h2>
    <p class="jms-sub">Enter the 6-character deck code from a classmate</p>
    <div class="jms-join-boxes" role="group" aria-label="Deck code">
        <input class="jms-join-box" type="text" maxlength="1" autocomplete="off" autocapitalize="characters" spellcheck="false" aria-label="Character 1">
        <input class="jms-join-box" type="text" maxlength="1" autocomplete="off" autocapitalize="characters" spellcheck="false" aria-label="Character 2">
        <input class="jms-join-box" type="text" maxlength="1" autocomplete="off" autocapitalize="characters" spellcheck="false" aria-label="Character 3">
        <input class="jms-join-box" type="text" maxlength="1" autocomplete="off" autocapitalize="characters" spellcheck="false" aria-label="Character 4">
        <input class="jms-join-box" type="text" maxlength="1" autocomplete="off" autocapitalize="characters" spellcheck="false" aria-label="Character 5">
        <input class="jms-join-box" type="text" maxlength="1" autocomplete="off" autocapitalize="characters" spellcheck="false" aria-label="Character 6">
    </div>

<button type="button" class="jms-copy jms-join-go" disabled>Start Deck</button>
    <div class="jms-join-fail">
        <p class="jms-join-msg" role="status"></p>
        <button type="button" class="jms-copy jms-join-retry">Retry</button>
    </div>

    <div class="jms-join-loading" role="status">
        <span class="jms-spinner" aria-hidden="true"></span>
        <span>Finding your deck…</span>
    </div>
</div>

<div class="jms-dialog jms-pub-dialog" role="dialog" aria-modal="true" aria-labelledby="jmsPubTitle" hidden>
    <button type="button" class="jms-x" aria-label="Close">×</button>
    <h2 id="jmsPubTitle">Publish a Deck</h2>
    <p class="jms-sub jms-pub-sub">Select a deck to share with the community</p>
    <div class="jms-pub-list" role="radiogroup" aria-labelledby="jmsPubTitle"></div>
    <p class="jms-pub-msg" role="status"></p>
    <button type="button" class="jms-copy jms-pub-go" disabled>Publish Deck</button>
</div>

<div class="jms-dialog jms-code-dialog" role="dialog" aria-modal="true" aria-labelledby="jmsCodeTitle" hidden>
    <button type="button" class="jms-x" aria-label="Close">×</button>
    <h2 id="jmsCodeTitle">Deck Code</h2>
    <p class="jms-sub">Share this code with your classmates</p>
    <p class="jms-code"></p>
    <button type="button" class="jms-copy jms-copy-code">Copy Code</button>
    <p class="jms-status" role="status"></p>
</div>`;

    let root = null;       // the dark overlay that holds both windows
    let ui = null;         // the parts we update
    let deck = null;       // the deck being shared (from the API)
    let opener = null;     // where focus goes back to when the window closes
    let copyTimer = null;
    let loading = false;

    function el(tag, cls, text) {
        const node = document.createElement(tag);
        if (cls) node.className = cls;
        if (text !== undefined) node.textContent = text;   // text only, so a deck name can never run as code
        return node;
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
            dialog: q('.jms-share-dialog'),
            codeDialog: q('.jms-code-dialog'),
            closes: [...root.querySelectorAll('.jms-x')],
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
            generate: q('.jms-generate'),
            codeText: q('.jms-code'),
            copyCode: q('.jms-copy-code'),
            status: q('.jms-code-dialog .jms-status'),
            joinDialog: q('.jms-join-dialog'),
                        joinBoxes: [...root.querySelectorAll('.jms-join-box')],
                        joinMsg: q('.jms-join-msg'),
                        joinGo: q('.jms-join-go'),
                        joinRetry: q('.jms-join-retry'),
                        pubDialog: q('.jms-pub-dialog'),
                        pubSub: q('.jms-pub-sub'),
                        pubList: q('.jms-pub-list'),
                        pubMsg: q('.jms-pub-msg'),
                        pubGo: q('.jms-pub-go')
        };

        ui.closes.forEach(b => b.addEventListener('click', close));
        root.addEventListener('click', e => { if (e.target === root) close(); });
        ui.dialog.addEventListener('click', e => { if (!ui.selectBox.contains(e.target)) toggleList(false); });
        ui.selectBtn.addEventListener('click', () => toggleList(ui.list.hidden));
        ui.options.forEach(b => b.addEventListener('click', () => {
            chooseShare(b.dataset.value);
            toggleList(false);
            ui.selectBtn.focus();
        }));
        ui.generate.addEventListener('click', onGenerate);
        ui.copyCode.addEventListener('click', onCopyCode);
        ui.pubGo.addEventListener('click', onPublishGo);
                setupJoin();
    }

    function toggleList(open) {
        ui.list.hidden = !open;
        ui.selectBtn.setAttribute('aria-expanded', String(open));
    }

    function showView(name) {
        ui.dialog.hidden = name !== 'share';
        ui.codeDialog.hidden = name !== 'code';
        ui.pubDialog.hidden = name !== 'publish';
                ui.joinDialog.hidden = name !== 'join';
    }

    // Updates the window for Public or Private. Nothing is saved here.
    function applyShare(value) {
        ui.selText.textContent = LABELS[value];
        ui.selIcon.innerHTML = ICONS[value];
        ui.options.forEach(b => b.setAttribute('aria-selected', String(b.dataset.value === value)));

        const isPublic = value === 'public';
        ui.generate.disabled = !isPublic;
        ui.hint.textContent = '';
        deck.share = value;
    }

    // The user picked Public or Private: save it, and go back if the server says no
    async function chooseShare(value) {
        const before = deck.share;
        if (value === before) return;
        applyShare(value);
        try {
            await JM.setDeckShare(deck.id, value);
        } catch (e) {
            applyShare(before);
            ui.hint.textContent = e.message || 'Could not save. Try again.';
        }
    }

    function onGenerate() {
        if (ui.generate.disabled) return;
        clearTimeout(copyTimer);
        ui.codeText.textContent = deck.shareCode;
        ui.copyCode.textContent = 'Copy Code';
        ui.status.textContent = '';
        showView('code');
        ui.copyCode.focus();
    }

    async function onCopyCode() {
        const ok = await copyText(deck.shareCode);
        clearTimeout(copyTimer);
        ui.copyCode.textContent = ok ? 'Code copied!' : 'Could not copy';
        ui.status.textContent = ui.copyCode.textContent;
        copyTimer = setTimeout(() => { ui.copyCode.textContent = 'Copy Code'; ui.status.textContent = ''; }, COPIED_MS);
    }

    function fill() {
        ui.cover.src = '/cover/cover ' + deck.cover + '.jpg';
        ui.name.textContent = deck.name;

        ui.tags.innerHTML = '';
        const n = deck.items;
        ui.tags.appendChild(el('span', 'jms-chip', n + (n === 1 ? ' Card' : ' Cards')));
        const types = deck.types || [];
        types.slice(0, MAX_TYPE_CHIPS).forEach(t => ui.tags.appendChild(el('span', 'jms-chip', TYPE_LABELS[t] || t)));
        if (types.length > MAX_TYPE_CHIPS) ui.tags.appendChild(el('span', 'jms-chip', '+' + (types.length - MAX_TYPE_CHIPS)));

        toggleList(false);
        applyShare(deck.share === 'private' ? 'private' : 'public');
    }

    function onKey(e) {
        if (e.key === 'Escape') {
            e.preventDefault();
            if (!ui.list.hidden) { toggleList(false); ui.selectBtn.focus(); } else close();
            return;
        }
        if (e.key === 'Tab') {   // keep Tab inside the window that is showing
            const dlg = [ui.dialog, ui.codeDialog, ui.pubDialog, ui.joinDialog].find(d => !d.hidden);
            const items = [...dlg.querySelectorAll('button:not([disabled]), input')].filter(b => !b.closest('[hidden]') && b.offsetParent !== null);
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

    async function open(id, from) {
        if (loading || (root && !root.hidden)) return false;
        loading = true;
        opener = from || document.activeElement;
        let found = null;
        try {
            found = await JM.deck(id);
        } catch (e) {
            found = null;
        } finally {
            loading = false;
        }
        if (!found) return false;
        if (!root) build();
        deck = found;
        fill();
        showView('share');
        root.hidden = false;
        document.addEventListener('keydown', onKey);
        ui.closes[0].focus();
        return true;
    }

    function close() {
        if (!root || root.hidden) return;
        root.hidden = true;
        clearTimeout(copyTimer);
        joinRun++;   // a request still running must not open a page after the window is closed
                document.removeEventListener('keydown', onKey);
        if (opener && typeof opener.focus === 'function') opener.focus();
    }

/* ---------- Publish a Deck window ---------- */
    let pubHasDecks = false;   // true when the user has any deck, Public or not
    let pubDecks = [];
    let pubPicked = null;   // id of the deck chosen in the list

    function markPicked() {
        ui.pubList.querySelectorAll('.jms-pub-item').forEach(b =>
            b.setAttribute('aria-checked', String(Number(b.dataset.id) === pubPicked)));
        ui.pubGo.disabled = pubPicked === null;
    }

    function pubItem(d) {
        const b = el('button', 'jms-pub-item');
        b.type = 'button';
        b.setAttribute('role', 'radio');
        b.setAttribute('aria-checked', 'false');
        b.dataset.id = d.id;

        const img = el('img', 'jms-cover');
        img.src = '/cover/cover ' + d.cover + '.jpg';
        img.alt = '';

        const info = el('div', 'jms-deck-info');
        info.appendChild(el('div', 'jms-name', d.name));
        const tags = el('div', 'jms-tags');
        tags.appendChild(el('span', 'jms-chip', d.items + (d.items === 1 ? ' Card' : ' Cards')));
        const types = d.types || [];
        types.slice(0, MAX_TYPE_CHIPS).forEach(t => tags.appendChild(el('span', 'jms-chip', TYPE_LABELS[t] || t)));
        if (types.length > MAX_TYPE_CHIPS) tags.appendChild(el('span', 'jms-chip', '+' + (types.length - MAX_TYPE_CHIPS)));
        info.appendChild(tags);

        b.append(img, info);
        b.addEventListener('click', () => { pubPicked = d.id; ui.pubMsg.textContent = ''; markPicked(); });
        return b;
    }
    function fillPublish() {
        ui.pubList.innerHTML = '';
        ui.pubMsg.textContent = '';
        pubPicked = null;
        ui.pubSub.hidden = !pubDecks.length;   // the hint only shows when there are decks to pick
        if (!pubDecks.length) {
            const empty = el('p', 'jms-pub-empty');
            if (pubHasDecks) {
                empty.textContent = 'No public decks yet. Only Public decks show up here.';
            } else {
                empty.append('You have no decks yet. ');
                const a = el('a', '', 'Create your first deck');
                a.href = '/pages/Loading-screen.html';
                empty.appendChild(a);
            }
            ui.pubList.appendChild(empty);
        } else {
            pubDecks.forEach(d => ui.pubList.appendChild(pubItem(d)));
        }
        markPicked();
    }

    function onPublishGo() {
        const d = pubDecks.find(x => x.id === pubPicked);
        if (!d || ui.pubGo.disabled) return;
        deck = d;
        clearTimeout(copyTimer);
        ui.codeText.textContent = d.shareCode;
        ui.copyCode.textContent = 'Copy Code';
        ui.status.textContent = '';
        showView('code');
        ui.copyCode.focus();
    }

    async function openPublish(from) {
        if (loading || (root && !root.hidden)) return false;
        loading = true;
        opener = from || document.activeElement;
        let list = null;
        try {
            list = await JM.decks();
        } catch (e) {
            list = null;
        } finally {
            loading = false;
        }
        if (!Array.isArray(list)) return false;
        if (!root) build();
        pubHasDecks = list.length > 0;
                pubDecks = list.filter(d => d.share === 'public');
        fillPublish();
        showView('publish');
        root.hidden = false;
        document.addEventListener('keydown', onKey);
        ui.pubDialog.querySelector('.jms-x').focus();
        return true;
    }

/* ---------- Play a Shared Deck window ---------- */
    const CODE_LENGTH = 6;

    function joinCode() {
        return ui.joinBoxes.map(b => b.value).join('');
    }

    function syncJoin() {
        ui.joinGo.disabled = joinCode().length !== CODE_LENGTH;
    }

    function cleanChars(s) {
        return s.toUpperCase().replace(/[^A-Z0-9]/g, '');
    }

    const MIN_LOADING_MS = 1000;   // the loading animation always shows at least this long
    let joinRun = 0;               // each Start Deck click gets a number, so a closed window is ignored

    // state: '' (type the code), 'loading' or 'failed'
    function setJoinState(state) {
        ui.joinDialog.classList.toggle('is-loading', state === 'loading');
        ui.joinDialog.classList.toggle('is-failed', state === 'failed');
        if (state === 'loading') ui.joinDialog.querySelector('.jms-x').focus();
        if (state === 'failed') ui.joinRetry.focus();
    }

    async function onStartDeck() {
        const code = joinCode();
        if (code.length !== CODE_LENGTH || ui.joinGo.disabled) return;
        const run = ++joinRun;
        ui.joinMsg.textContent = '';
        setJoinState('loading');

        let found = null;
        let failure = '';
        const wait = new Promise(resolve => setTimeout(resolve, MIN_LOADING_MS));
        try {
            [found] = await Promise.all([JM.deckByCode(code), wait]);
        } catch (e) {
            failure = e.message || 'Could not find that deck. Try again.';
            await wait;
        }
        if (run !== joinRun) return;   // the window was closed meanwhile

        if (found) {
            location.href = '/pages/Deck overview.html?id=' + encodeURIComponent(found.id);
            return;   // the spinner stays until the next page loads
        }
        ui.joinMsg.textContent = failure;
        setJoinState('failed');
    }

    // Retry: back to the 6 empty boxes
    function onRetry() {
        ui.joinBoxes.forEach(b => { b.value = ''; });
        ui.joinMsg.textContent = '';
        syncJoin();
        setJoinState('');
        ui.joinBoxes[0].focus();
    }

    // Coming back with the browser Back button must not show a window stuck on the spinner
    window.addEventListener('pageshow', e => {
        if (e.persisted && ui && ui.joinDialog) { setJoinState(''); close(); }
    });

    function setupJoin() {
        const boxes = ui.joinBoxes;
        boxes.forEach((box, i) => {
            box.addEventListener('focus', () => box.select());
            box.addEventListener('input', () => {
                box.value = cleanChars(box.value).slice(0, 1);
                ui.joinMsg.textContent = '';
                if (box.value && i < boxes.length - 1) boxes[i + 1].focus();
                syncJoin();
            });
            box.addEventListener('keydown', e => {
                if (e.key === 'Backspace' && !box.value && i > 0) {
                    e.preventDefault();
                    boxes[i - 1].value = '';
                    boxes[i - 1].focus();
                    syncJoin();
                } else if (e.key === 'ArrowLeft' && i > 0) {
                    e.preventDefault();
                    boxes[i - 1].focus();
                } else if (e.key === 'ArrowRight' && i < boxes.length - 1) {
                    e.preventDefault();
                    boxes[i + 1].focus();
                } else if (e.key === 'Enter') {
                    e.preventDefault();
                    onStartDeck();
                }
            });
            box.addEventListener('paste', e => {
                e.preventDefault();
                const text = cleanChars((e.clipboardData || window.clipboardData).getData('text')).slice(0, boxes.length);
                boxes.forEach((b, k) => { b.value = text[k] || ''; });
                ui.joinMsg.textContent = '';
                boxes[Math.min(text.length, boxes.length - 1)].focus();
                syncJoin();
            });
        });
        ui.joinGo.addEventListener('click', onStartDeck);
        ui.joinRetry.addEventListener('click', onRetry);
    }

    function openJoin(from) {
        if (loading || (root && !root.hidden)) return false;
        opener = from || document.activeElement;
        if (!root) build();
        ui.joinBoxes.forEach(b => { b.value = ''; });
        setJoinState('');
        ui.joinMsg.textContent = '';
        syncJoin();
        showView('join');
        root.hidden = false;
        document.addEventListener('keydown', onKey);
        ui.joinBoxes[0].focus();
        return true;
    }

    window.JMShare = { open: open, openPublish: openPublish, openJoin: openJoin, close: close };
})();