/* ==========================================================================
   Java Master - lesson engine
   --------------------------------------------------------------------------
   Every lesson page loads this file. It drives the whole lesson:

     1. loads the node and its questions from the API (the payload never
        contains an answer, only what the learner is allowed to see),
     2. renders one question frame at a time,
     3. sends every answer to the server, which decides whether it is right
        and replies with the praise or the correction,
     4. lets the learner retry a question they got wrong,
     5. asks the server to complete the node, which pays the rewards, unlocks
        the next lesson and records the streak.

   The browser never decides whether an answer is correct.
   ========================================================================== */
(function () {
    'use strict';

    var NODE_ID = window.LESSON_NODE_ID;

    var MODEL = {
        wave: '/model/James Gosling wave.png',
        stand: '/model/James Gosling Standing Up.png',
        applause: '/model/James Gosling Applause.png',
        thinking: '/model/James Gosling Thinking.png',
        drink: '/model/James Gosling Drinking Coffee.png'
    };

    /* ---------------------------------------------------------------- state */
    var node = null;        // the node, straight from the server
    var questions = [];     // the questions, in order
    var screen = 'intro';   // 'intro' | 'question' | 'done'
    var qIndex = 0;
    var phase = 'answer';   // 'answer' | 'correct' | 'wrong'
    var sel = null;         // selected option id (multiple choice)
    var typed = '';         // free text (fill in the blank)
    var slots = {};         // slot id -> chosen token (fill slots / arrange tokens)
    var slotOrder = [];     // slot ids in the order they appear
    var serverOutput = null;// the output panel, only sent back on a correct answer
    var completion = null;  // the completion payload from the server
    var correcting = false; // true while the learner is fixing a wrong answer
    var busy = false;

    /* ------------------------------------------------------------- helpers */
    var $ = function (id) { return document.getElementById(id); };
    var esc = function (s) {
        return String(s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    };

    function pref(k) { try { return localStorage.getItem(k) !== 'false'; } catch (e) { return true; } }
    function setPref(k, v) {
        try { localStorage.setItem(k, v); } catch (e) { }
        if (k === 'backgroundMusicEnabled') { v ? playBg() : $('bgMusic').pause(); }
    }
    function playBg() {
        if (!pref('backgroundMusicEnabled')) return;
        var a = $('bgMusic'); a.volume = .3;
        a.play().catch(function () {
            document.addEventListener('click', function () {
                if (pref('backgroundMusicEnabled')) a.play();
            }, { once: true });
        });
    }
    function sfx(id) {
        if (!pref('soundEffectsEnabled')) return;
        var a = $(id); a.currentTime = 0; a.play().catch(function () { });
    }
    function openModal(id) { $(id).classList.add('on'); }
    function closeModal(id) { $(id).classList.remove('on'); }
    document.querySelectorAll('.modal').forEach(function (m) {
        m.addEventListener('click', function (e) { if (e.target === m) m.classList.remove('on'); });
    });

    function api(path, options) {
        return fetch('/api' + path, {
            headers: { 'Content-Type': 'application/json' },
            ...(options || {})
        }).then(function (res) {
            return res.json().catch(function () { return {}; }).then(function (data) {
                if (!res.ok) {
                    var err = new Error(data.error || 'Something went wrong.');
                    err.status = res.status;
                    throw err;
                }
                return data;
            });
        });
    }

    /* ------------------------------------------------------- question parts */
    function payloadOf(q) {
        // The API sends the payload as a JSON string.
        if (typeof q.payload === 'string') {
            try { return JSON.parse(q.payload); } catch (e) { return {}; }
        }
        return q.payload || {};
    }

    function slotsOf(p) {
        var p2 = p.lines || [];
        var ids = [];
        p2.forEach(function (line) {
            line.forEach(function (tok) {
                var m = /^\{\{(\d+)\}\}$/.exec(tok);
                if (m && ids.indexOf(m[1]) < 0) ids.push(m[1]);
            });
        });
        return ids;
    }

    function bankOf(p, type) {
        var bank = type === 'ArrangeTokens' ? (p.tokens || []) : (p.bank || []);
        if (type !== 'ArrangeTokens') return bank.slice();
        // The order matters for arrange-tokens, so do not hand them over in order.
        var shuffled = bank.slice();
        for (var i = shuffled.length - 1; i > 0; i--) {
            var j = Math.floor(Math.random() * (i + 1));
            var tmp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = tmp;
        }
        return shuffled;
    }

    function chip(tok) {
        if (tok === '.') return '<span class="dot">.</span>';
        return '<span class="chip">' + esc(tok) + '</span>';
    }

    function slotChip(id) {
        var v = slots[id];
        return v !== undefined
            ? '<span class="chip">' + esc(v) + '</span>'
            : '<span class="chip empty">____</span>';
    }

    function renderLines(lines) {
        return lines.map(function (line) {
            return '<div class="line">' + line.map(function (tok) {
                var m = /^\{\{(\d+)\}\}$/.exec(tok);
                return m ? slotChip(m[1]) : chip(tok);
            }).join('') + '</div>';
        }).join('');
    }

    function renderTemplate(template) {
        return template.map(function (tok) {
            var i = tok.indexOf('{{blank}}');
            if (i < 0) return chip(tok);
            var pre = tok.slice(0, i);
            var post = tok.slice(i + 9);
            // The input is wired up with a real event listener in render(), not an
            // inline handler: an inline handler would assign to the global scope
            // and the value would never reach this module.
            // Every typed answer goes through here, in every lesson, so this is
            // where the clipboard is closed off: the answer is usually in the
            // question text, so the field must not accept a paste of it.
            return '<span class="chip in">' + esc(pre) +
                '<input id="tin" ' + (phase === 'answer' ? '' : 'readonly') +
                ' value="' + esc(typed) + '" autocomplete="off" autocorrect="off" autocapitalize="off" spellcheck="false"' +
                ' onpaste="return false" ondrop="return false" oncopy="return false" oncut="return false" oncontextmenu="return false">' +
                esc(post) + '</span>';
        }).join('');
    }

    function workspace(inner, tools) {
        var out = (phase === 'correct' && serverOutput)
            ? '<div class="output"><div>Output</div><div>' + esc(serverOutput) + '</div></div>'
            : '';
        var tl = (tools && phase === 'answer')
            ? '<div class="tools"><button data-action="rem"><img src="/icons/remove.png" alt="">Remove</button>' +
              '<button data-action="reset"><img src="/icons/reset.png" alt="">Reset</button></div>'
            : (tools ? '<div class="tools"></div>' : '');
        return '<div class="work"><div class="editor"><div class="tab"><img src="/icons/small size java logo.png" alt="">Main.java</div>' +
            '<div class="code">' + inner + '</div>' + tl + '</div>' + out + '</div>';
    }

    /* ------------------------------------------------------------ rendering */
    function render() {
        var st = $('stage');
        var b = $('bubbles');
        b.innerHTML = '';
        var m = $('model');
        m.style.display = screen === 'done' ? 'none' : 'block';
        var img = MODEL.stand;

        $('topBar').style.display = screen === 'done' ? 'none' : 'flex';
        $('progress').style.display = screen === 'question' ? 'flex' : 'none';

        // The finished screen has no top bar and no progress dots, so the
        // room they needed at the top is given back and the panel can sit in
        // the middle of the screen. The finish screen keeps the top bar, so
        // the learner can still go back.
        st.classList.toggle('center', screen === 'done');

        if (screen === 'question') {
            $('progress').innerHTML = questions.map(function (_, i) {
                return '<i class="' + (i <= qIndex ? 'on' : '') + '"></i>';
            }).join('');
        }

        if (screen === 'finish') { renderFinish(); return; }

        if (screen === 'intro') {
            img = MODEL.wave;
            m.src = img;
            st.innerHTML = '<div class="text">' +
                (node.introText || '').split(/\n\n/).map(function (t) { return '<p>' + esc(t) + '</p>'; }).join('') +
                '</div><button class="btn mid" data-action="start">Next</button>';
            return;
        }

        if (screen === 'done') { renderDone(); return; }

        var q = questions[qIndex];
        var p = payloadOf(q);
        slotOrder = slotsOf(p);
        var body = '';

        if (phase === 'correct') { img = MODEL.applause; b.innerHTML = '<span>' + esc(lastMessage || '') + '</span>'; }
        else if (phase === 'wrong' || correcting) {
            // The correction stays on screen while the learner fixes the answer.
            img = MODEL.thinking;
            b.innerHTML = '<span>Something’s not right</span><span>' + esc(lastMessage || '') + '</span>';
        }
        m.src = img;

        if (q.type === 'MultipleChoice') {
            body = '<div class="opts">' + (p.options || []).map(function (o) {
                var c = 'opt';
                if (phase === 'answer' && sel === o.id) c += ' sel';
                if (sel === o.id && phase === 'correct') c += ' ok';
                if (sel === o.id && phase === 'wrong') c += ' bad';
                return '<button class="' + c + '" ' + (phase !== 'answer' ? 'disabled' : '') +
                    ' data-opt="' + esc(o.id) + '">' + esc(o.text) + '</button>';
            }).join('') + '</div>';
        } else if (q.type === 'FillBlank') {
            body = workspace('<div class="line">' + renderTemplate(p.template || []) + '</div>', false);
        } else {
            // FillSlots and ArrangeTokens both fill marked places from a bank of tokens.
            body = workspace(renderLines(p.lines || []), true) +
                '<div class="choices">' + bankOf(p, q.type).map(function (t) {
                    var used = Object.keys(slots).some(function (k) { return slots[k] === t; });
                    return '<button class="choice" ' + (used || phase !== 'answer' ? 'disabled' : '') +
                        ' data-token="' + esc(t) + '">' + esc(t) + '</button>';
                }).join('') + '</div>';
        }

        st.innerHTML = '<div class="prompt"><p>' + esc(q.prompt || '').replace(/\n\n/g, '</p><p>') + '</p></div>' +
            body + '<button class="btn" id="mainBtn"></button>';

        if (q.type === 'FillBlank' && phase === 'answer') {
            var ti = $('tin');
            if (ti) {
                ti.addEventListener('input', function () { typed = ti.value; upd(); });
                ti.focus();
            }
        }
        upd();
    }

    // The finish screen comes between the last question and the completed
    // screen. It is only shown the first time a lesson is finished, so a
    // replay goes straight to the completed screen.
    function renderFinish() {
        var c = completion || {};

        // The name comes from the server's own record of what was just
        // completed, so it can never disagree with the lesson that was played.
        var title = c.title || (node && node.title) || 'the lesson';

        // The first line says the lesson is done and the next one is open.
        // The rest is the lesson's own closing text, which the server owns.
        var outro = (c.outroText || '').split(/\n\n/).filter(function (t) { return t.trim(); });

        $('model').src = MODEL.drink;
        $('stage').innerHTML = '<div class="text finish">' +
            '<p>' + esc('Great Job! You finished ' + title + '. The next lesson is now unlocked.') + '</p>' +
            outro.map(function (t) { return '<p>' + esc(t) + '</p>'; }).join('') +
            '</div><button class="btn mid" data-action="finishNext">Next</button>';
    }

    function renderDone() {
        var c = completion || {};

        // A lesson that is played for the first time shows what it paid, so
        // the learner can claim the reward. A replay has nothing left to
        // claim, so it gets the plain screen with the closing line instead.
        var first = c.firstTime;

        var body = first
            ? '<div class="rew"><div class="l"><b id="rankName">' + esc(c.rankBefore || 'Starter') + '</b>' +
              '<b id="xpNum">' + (c.xpBefore || 0) + '</b>' +
              '<b>Coins</b><b>+' + (c.coinsEarned || 0) + '</b></div>' +
              '<div class="earn">EARNED<div>' + (c.xpEarned || 0) + ' XP</div></div></div>'
            : '<p class="done-desc">Nice review! Rewards can only be earned once per lesson.</p>';

        $('stage').innerHTML = '<div class="done">' +
            '<img src="/icons/' + (first ? 'victory.png' : 'done.png') + '" alt="">' +
            '<h1>COMPLETED!</h1>' + body +
            '<button class="rbtn" id="rewardBtn">' + (first ? 'Get Reward' : 'Continue') + '</button></div>';
        $('bubbles').innerHTML = '';
        $('bgMusic').pause();
        sfx('completedSound');

        if (!first) return;

        // The XP counts up to the new total, and the rank name changes once it
        // has landed.
        countUp($('xpNum'), c.xpBefore || 0, c.xpAfter || 0, 1200);
        setTimeout(function () {
            var r = $('rankName');
            if (r) r.textContent = c.rankAfter || 'Starter';
        }, 1250);
    }

    // Counts a number up from one value to another.
    function countUp(el, from, to, d) {
        if (!el) return;
        // Someone who asked for less motion gets the number straight away.
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            el.textContent = to;
            return;
        }
        var t0 = performance.now();
        (function f(n) {
            var t = Math.min((n - t0) / d, 1);
            var e = 1 - Math.pow(1 - t, 3);
            el.textContent = Math.round(from + (to - from) * e);
            if (t < 1) requestAnimationFrame(f);
        })(t0);
    }

    /* ----------------------------------------------------------- answering */
    function upd() {
        var b = $('mainBtn');
        if (!b) return;
        var q = questions[qIndex];
        if (phase === 'correct') { b.textContent = 'Continue'; b.disabled = false; return; }
        if (phase === 'wrong') { b.textContent = 'Try Again'; b.disabled = false; return; }
        b.textContent = 'Check';
        if (q.type === 'MultipleChoice') b.disabled = sel === null;
        else if (q.type === 'FillBlank') b.disabled = !typed.trim();
        else b.disabled = slotOrder.some(function (id) { return slots[id] === undefined; });
    }

    function pick(id) {
        sel = id;
        var opts = payloadOf(questions[qIndex]).options || [];
        document.querySelectorAll('.opt').forEach(function (o, k) {
            o.classList.toggle('sel', !!(opts[k] && opts[k].id === id));
        });
        upd();
    }

    function addTok(token) {
        var q = questions[qIndex];
        var p = payloadOf(q);
        var id = slotOrder.find(function (s) { return slots[s] === undefined; });
        if (!id) return;
        slots[id] = token;
        render();
    }

    function rem() {
        var last = slotOrder.filter(function (s) { return slots[s] !== undefined; }).pop();
        if (last) delete slots[last];
        render();
    }

    function reset() { slots = {}; render(); }

    function answerBody() {
        var q = questions[qIndex];
        if (q.type === 'MultipleChoice') return { optionId: sel };
        if (q.type === 'FillBlank') return { text: typed };
        if (q.type === 'ArrangeTokens') {
            return { order: slotOrder.map(function (id) { return slots[id]; }) };
        }
        var out = {};
        slotOrder.forEach(function (id) { out[id] = slots[id]; });
        return { slots: out };
    }

    var lastMessage = '';

    function mainAction() {
        if (busy) return;
        if (phase === 'correct') { next(); return; }
        if (phase === 'wrong') { retry(); return; }

        var q = questions[qIndex];
        busy = true;
        api('/learn/questions/' + q.id + '/check', {
            method: 'POST',
            body: JSON.stringify({ nodeId: NODE_ID, answer: answerBody() })
        }).then(function (res) {
            busy = false;
            lastMessage = res.message || '';
            serverOutput = res.output || null;
            correcting = !res.correct;
            phase = res.correct ? 'correct' : 'wrong';
            if (res.correct) sfx('correctSound'); else sfx('wrongSound');
            render();
        }).catch(function (err) {
            busy = false;
            lastMessage = err.message || 'Something went wrong.';
            correcting = true;
            phase = 'wrong';
            render();
        });
    }

    function retry() {
        phase = 'answer';
        // The model goes back to standing and the correction goes away, so the
        // learner sees a clean question again instead of the same wrong answer
        // still being on screen.
        correcting = false;
        lastMessage = '';
        var q = questions[qIndex];
        // Keep the typed text so a small slip does not have to be typed again,
        // but clear the choices so a wrong option is not simply left selected.
        if (q.type === 'MultipleChoice') sel = null;
        if (q.type === 'FillSlots' || q.type === 'ArrangeTokens') slots = {};
        serverOutput = null;
        render();
    }

    function next() {
        if (qIndex >= questions.length - 1) { finish(); return; }
        qIndex++;
        phase = 'answer';
        correcting = false;
        sel = null;
        typed = '';
        slots = {};
        serverOutput = null;
        lastMessage = '';
        render();
    }

    function startLesson() {
        screen = 'question';
        qIndex = 0;
        phase = 'answer';
        correcting = false;
        sel = null; typed = ''; slots = {}; serverOutput = null; lastMessage = '';
        render();
    }

    /* ----------------------------------------------------------- completing */
    function finish() {
        busy = true;
        api('/learn/nodes/' + NODE_ID + '/complete', { method: 'POST', body: '{}' })
            .then(function (res) {
                busy = false;
                completion = res;
                // The finish screen is the celebration, and it only happens the
                // first time. A replay goes straight to the completed screen.
                screen = res.firstTime ? 'finish' : 'done';
                render();
            })
            .catch(function (err) {
                busy = false;
                lastMessage = err.message || 'Could not finish the lesson.';
                screen = 'question';
                render();
            });
    }



    var claimed = false;
    function getReward() {
        if (claimed) return;
        claimed = true;
        var b = $('rewardBtn');
        var c = completion || {};
        if ((c.xpEarned || 0) > 0) {
            b.textContent = 'Reward Claimed';
            b.classList.add('claimed');
            b.disabled = true;
            var r = b.getBoundingClientRect();
            coinBurst(r.left + r.width / 2, r.top + r.height / 2);
        }
        // The Learn page shows the "+1 Badge" toast when it is told a badge was earned.
        var target = (completion && completion.badgeEarned) ? '/pages/Learn.html?badge=1' : '/pages/Learn.html';
        setTimeout(function () { location.href = target; }, 2300);
    }

    /* -------------------------------------------------------- coin burst */
    var cv = $('coins'), ctx = cv.getContext('2d');
    var W = 0, H = 0, coins = [], sparks = [], playing = false, t0 = null, tl = null;
    var DURATION = 2000;
    var rand = function (a, b) { return a + Math.random() * (b - a); };
    var gold = ["#FFF2A1", "#FFDC57", "#E7A91D", "#D28A0B", "#FFF0A0"];

    function resize() {
        var d = Math.min(devicePixelRatio || 1, 2);
        W = innerWidth; H = innerHeight;
        cv.width = W * d; cv.height = H * d;
        ctx.setTransform(d, 0, 0, d, 0, 0);
    }
    function burst(x, y) {
        sparks.push({ x: x, y: y, r: rand(22, 34), life: 1 });
        var n = Math.round(Math.min(100, Math.max(55, W / 10)));
        for (var i = 0; i < n; i++) {
            var a = rand(-Math.PI, Math.PI), sp = rand(2.5, 10.5);
            coins.push({
                x: x + rand(-7, 7), y: y + rand(-7, 7), vx: Math.cos(a) * sp, vy: Math.sin(a) * sp - rand(1, 3),
                r: rand(7, 13), depth: rand(.65, 1.2), angle: rand(0, 6.28), spin: rand(-.2, .2),
                rot: rand(-.1, .1), color: gold[Math.floor(Math.random() * gold.length)], age: 0, wobble: rand(0, 6.28)
            });
        }
    }
    function drawCoin(c) {
        var p = Math.max(.12, Math.abs(Math.cos(c.angle))), rx = c.r * p * c.depth, ry = c.r * c.depth;
        ctx.save(); ctx.translate(c.x, c.y); ctx.rotate(c.rot);
        ctx.beginPath(); ctx.ellipse(1.2, 1.8, rx + 1.3, ry + 1.3, 0, 0, 6.283); ctx.fillStyle = "#805008"; ctx.fill();
        var g = ctx.createLinearGradient(-rx, -ry, rx, ry);
        g.addColorStop(0, "#FFF4A4"); g.addColorStop(.22, "#FFD94B"); g.addColorStop(.48, c.color);
        g.addColorStop(.72, "#D28A0B"); g.addColorStop(1, "#FFF0A0");
        ctx.beginPath(); ctx.ellipse(0, 0, Math.max(.8, rx), ry, 0, 0, 6.283); ctx.fillStyle = g; ctx.fill();
        ctx.lineWidth = Math.max(.6, c.r * .09); ctx.strokeStyle = "rgba(255,245,166,.95)"; ctx.stroke();
        if (p > .38) {
            ctx.beginPath(); ctx.ellipse(0, 0, Math.max(.4, rx * .72), ry * .72, 0, 0, 6.283);
            ctx.lineWidth = Math.max(.45, c.r * .055); ctx.strokeStyle = "rgba(155,93,5,.7)"; ctx.stroke();
            ctx.save(); ctx.scale(p, 1); ctx.font = 'bold ' + Math.max(7, c.r * 1.25) + 'px Arial';
            ctx.textAlign = "center"; ctx.textBaseline = "middle";
            ctx.fillStyle = "rgba(133,79,0,.7)"; ctx.fillText("$", .6, 1);
            ctx.fillStyle = "rgba(255,249,190,.98)"; ctx.fillText("$", -.3, -.4); ctx.restore();
        }
        ctx.beginPath(); ctx.ellipse(-rx * .28, -ry * .36, Math.max(.5, rx * .2), Math.max(.45, ry * .11), -.35, 0, 6.283);
        ctx.fillStyle = "rgba(255,255,226,.78)"; ctx.fill();
        ctx.restore();
    }
    function glow() {
        ctx.clearRect(0, 0, W, H);
        var f = Math.max(0, 1 - (performance.now() - t0) / 420);
        if (f > 0) {
            var g = ctx.createRadialGradient(W / 2, H / 2, 0, W / 2, H / 2, Math.max(W, H) * .42);
            g.addColorStop(0, 'rgba(255,195,45,' + f * .14 + ')');
            g.addColorStop(1, 'rgba(255,195,45,0)');
            ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
        }
    }
    function updSparks(dt) {
        for (var i = sparks.length - 1; i >= 0; i--) {
            var s = sparks[i]; s.life -= .045 * dt; s.r *= Math.pow(1.09, dt);
            if (s.life <= 0) { sparks.splice(i, 1); continue; }
            var g = ctx.createRadialGradient(s.x, s.y, 0, s.x, s.y, s.r);
            g.addColorStop(0, 'rgba(255,226,106,' + s.life * .65 + ')');
            g.addColorStop(.25, 'rgba(255,177,20,' + s.life * .2 + ')');
            g.addColorStop(1, 'rgba(255,170,0,0)');
            ctx.fillStyle = g; ctx.fillRect(s.x - s.r, s.y - s.r, s.r * 2, s.r * 2);
        }
    }
    function updCoins(dt) {
        for (var i = coins.length - 1; i >= 0; i--) {
            var c = coins[i]; c.age += dt;
            c.vy += .105 * dt; c.vx *= Math.pow(.998, dt);
            c.x += c.vx * dt; c.y += c.vy * dt;
            c.angle += c.spin * dt; c.rot += c.spin * .13 * dt; c.wobble += .04 * dt;
            c.x += Math.sin(c.wobble) * .18 * dt;
            ctx.globalAlpha = Math.max(0, Math.min(Math.min(1, c.age / 5), Math.min(1, (120 - c.age) / 30)));
            drawCoin(c); ctx.globalAlpha = 1;
            if (c.age >= 120 || c.y > H + 60 || c.x < -70 || c.x > W + 70) coins.splice(i, 1);
        }
    }
    function loop(ts) {
        if (!playing) return;
        if (tl === null) tl = ts;
        if (ts - t0 >= DURATION) { playing = false; coins = []; sparks = []; ctx.clearRect(0, 0, W, H); return; }
        var dt = Math.min(2, Math.max(0, (ts - tl) / 16.67)); tl = ts;
        glow(); updSparks(dt); updCoins(dt); requestAnimationFrame(loop);
    }
    function coinBurst(x, y) {
        coins = []; sparks = []; t0 = performance.now(); tl = null; playing = true; burst(x, y); requestAnimationFrame(loop);
    }
    addEventListener('resize', resize); resize();

    /* ------------------------------------------------------------- exports */
    // One delegated listener handles every button inside the stage. Inline
    // handlers cannot see this module's variables, and they break on tokens
    // that contain quotation marks.
    $('stage').addEventListener('click', function (e) {
        var b = e.target.closest ? e.target.closest('button') : null;
        if (!b || b.disabled) return;
        if (b.dataset.opt) { pick(b.dataset.opt); return; }
        if (b.dataset.token !== undefined) { addTok(b.dataset.token); return; }
        if (b.dataset.action === 'start') { startLesson(); return; }
        if (b.dataset.action === 'finishNext') { screen = 'done'; render(); return; }
        if (b.dataset.action === 'rem') { rem(); return; }
        if (b.dataset.action === 'reset') { reset(); return; }
        if (b.dataset.action === 'back') { location.href = '/pages/Learn.html'; return; }
        if (b.id === 'mainBtn') { mainAction(); return; }
        if (b.id === 'rewardBtn') { getReward(); return; }
    });

    window.pick = pick;
    window.addTok = addTok;
    window.rem = rem;
    window.reset = reset;
    window.mainAction = mainAction;
    window.getReward = getReward;
    window.startLesson = startLesson;
    window.upd = upd;
    window.openModal = openModal;
    window.closeModal = closeModal;
    window.setPref = setPref;

    /* ---------------------------------------------------------------- boot */
    function boot() {
        $('bgT').checked = pref('backgroundMusicEnabled');
        $('sfxT').checked = pref('soundEffectsEnabled');
        playBg();

        // Ask for the path first. It says whether this node is open, so a locked
        // lesson can be turned away without ever calling the node endpoint and
        // producing a 403 in the console.
        api('/learn/path').then(function (path) {
            var state = null;
            (path || []).forEach(function (s) {
                (s.nodes || []).forEach(function (n) { if (n.id === NODE_ID) state = n.status; });
            });

            if (state === 'Locked') {
                var locked = new Error('This lesson is still locked.');
                locked.status = 403;
                throw locked;
            }
            return api('/learn/nodes/' + NODE_ID).then(function (nodeData) {
                return { path: path, node: nodeData };
            });
        }).then(function (res) {
            var nodeData = res.node;
            node = nodeData;
            questions = nodeData.questions || [];

            var q = questions[0];
            if (q) slotOrder = slotsOf(payloadOf(q));

            render();
        }).catch(function (err) {
            var m = $('model');
            if (m) m.src = MODEL.stand;
            var locked = err.status === 403;
            $('stage').innerHTML = '<div class="text"><p>' +
                (locked ? 'This lesson is still locked.' : 'This lesson could not be loaded.') + '</p><p>' +
                esc(locked ? 'Finish the lesson before it on the path to unlock this one.'
                           : (err.message || '')) +
                '</p></div><button class="btn mid" data-action="back">Back to the path</button>';
        });
    }

    window.onload = boot;
})();