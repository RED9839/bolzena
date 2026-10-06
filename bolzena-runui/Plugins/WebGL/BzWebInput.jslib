// 웹(WebGL) 글자 입력 — 유니티 TMP_InputField 는 브라우저 IME(한글 조합 · 초성)를 못 받는다.
// 입력 칸 자리에 투명한 HTML <input> 을 겹쳐 놓고(브라우저 IME 가 그 칸에 조합 · 후보창을 띄운다), 값이 바뀔 때마다 유니티로 넘긴다
// (조합 중 글자도 — TMP 칸이 그리고, 투명한 input 은 입력만 받는다). 폰은 이 input 에 초점이 가므로 화면 자판이 그대로 뜬다.
// 짝: Runtime/Kit/WebInput.cs (C# 쪽). 받는 쪽 오브젝트 이름 "BzWebInput", 메서드 OnValue · OnEnd · OnArmed.
// 메시지는 \u0001 로 나눈 글: OnValue = id \u0001 커서 \u0001 값 / OnEnd = id / OnArmed = id.
var BzWebInputLib = {
  $BzWI: {
    host: 'BzWebInput',
    el: null,
    openId: -1,
    reg: {},
    hooked: false,
    armedAt: 0,
    canvas: function () {
      return (typeof Module !== 'undefined' && Module.canvas) || document.getElementById('unity-canvas') || document.querySelector('canvas');
    },
    send: function (method, msg) {
      var f = (typeof Module !== 'undefined' && Module.SendMessage) || (window.unityInstance && window.unityInstance.SendMessage && window.unityInstance.SendMessage.bind(window.unityInstance));
      if (f) f(BzWI.host, method, msg);
    },
    css: function (r) {
      var c = BzWI.canvas().getBoundingClientRect();
      return { x: c.left + r.x * c.width, y: c.top + r.y * c.height, w: r.w * c.width, h: r.h * c.height, fs: r.fs * c.height };
    },
    make: function () {
      if (BzWI.el) return BzWI.el;
      var e = document.createElement('input');
      e.type = 'text';
      e.autocomplete = 'off';
      e.spellcheck = false;
      e.setAttribute('autocorrect', 'off');
      e.setAttribute('autocapitalize', 'off');
      e.setAttribute('enterkeyhint', 'search');
      e.setAttribute('aria-label', '검색');
      var s = e.style;
      s.position = 'fixed'; s.zIndex = '2147483000'; s.margin = '0'; s.padding = '0'; s.border = '0'; s.outline = 'none';
      s.background = 'transparent'; s.color = 'transparent'; s.caretColor = 'transparent'; s.webkitTextFillColor = 'transparent';
      s.boxSizing = 'border-box'; s.display = 'none'; s.fontFamily = 'sans-serif';
      var push = function () {
        if (BzWI.openId < 0) return;
        BzWI.send('OnValue', BzWI.openId + '\u0001' + (e.selectionStart == null ? e.value.length : e.selectionStart) + '\u0001' + e.value);
      };
      e.addEventListener('input', push);
      document.addEventListener('selectionchange', function () { if (document.activeElement === e) push(); });
      var stop = function (ev) { ev.stopPropagation(); };   // 유니티가 이 키를 가로채(preventDefault) 글이 안 써지는 일이 없게
      e.addEventListener('keydown', function (ev) {
        ev.stopPropagation();
        if ((ev.key === 'Enter' || ev.key === 'Escape') && !ev.isComposing && ev.keyCode !== 229) { ev.preventDefault(); BzWI.close(true); }
      });
      e.addEventListener('keyup', stop);
      e.addEventListener('keypress', stop);
      e.addEventListener('blur', function () {
        // 탭 직후 유니티가 캔버스로 초점을 가져가는 경우 — 곧바로 되찾는다
        if (Date.now() - BzWI.armedAt < 400 && BzWI.openId >= 0) { setTimeout(function () { if (BzWI.openId >= 0) e.focus(); }, 0); return; }
        BzWI.close(true);
      });
      document.body.appendChild(e);
      BzWI.el = e;
      return e;
    },
    place: function (r) {
      var e = BzWI.make(), p = BzWI.css(r), s = e.style;
      s.left = p.x + 'px'; s.top = p.y + 'px'; s.width = p.w + 'px'; s.height = p.h + 'px';
      s.fontSize = Math.max(8, p.fs) + 'px'; s.lineHeight = p.h + 'px';
      s.display = 'block';
    },
    open: function (id, focus) {
      var r = BzWI.reg[id];
      if (!r) return;
      BzWI.place(r);
      BzWI.openId = id;
      if (focus) { try { BzWI.el.focus({ preventScroll: true }); } catch (x) { BzWI.el.focus(); } }
    },
    close: function (notify) {
      var id = BzWI.openId;
      if (!BzWI.el || id < 0) return;
      BzWI.openId = -1;
      BzWI.el.style.display = 'none';
      if (document.activeElement === BzWI.el) BzWI.el.blur();
      if (notify) BzWI.send('OnEnd', String(id));
    },
    hook: function () {
      if (BzWI.hooked) return;
      var c = BzWI.canvas();
      if (!c) return;
      BzWI.hooked = true;
      // 탭 · 클릭이 끝나는 그 자리(사용자 동작 안)에서 초점을 줘야 iOS 가 화면 자판을 띄운다 — 유니티가 선택을 알아채기 전에 먼저 건다
      c.addEventListener('pointerup', function (ev) {
        for (var k in BzWI.reg) {
          var r = BzWI.reg[k];
          var p = BzWI.css(r);
          if (ev.clientX >= p.x && ev.clientX <= p.x + p.w && ev.clientY >= p.y && ev.clientY <= p.y + p.h) {
            BzWI.armedAt = Date.now();
            BzWI.open(parseInt(k, 10), true);
            BzWI.send('OnArmed', String(k));
            return;
          }
        }
      }, true);
    }
  },

  // 입력 칸 자리 알림 — x y w h 는 캔버스 안 0~1(위가 0), fs 는 글자 높이(캔버스 높이 비)
  BzWI_Reg: function (id, x, y, w, h, fs) {
    BzWI.hook();
    BzWI.reg[id] = { x: x, y: y, w: w, h: h, fs: fs };
    if (BzWI.openId === id) BzWI.place(BzWI.reg[id]);
  },
  BzWI_Unreg: function (id) {
    delete BzWI.reg[id];
    if (BzWI.openId === id) BzWI.close(false);
  },
  // 칸을 열고 초점 · 값을 맞춘다. maxLen 0 = 제한 없음
  BzWI_Show: function (id, textPtr, maxLen, caret) {
    BzWI.hook();
    BzWI.open(id, true);
    var e = BzWI.el;
    if (!e || BzWI.openId !== id) return;
    var t = UTF8ToString(textPtr);
    e.maxLength = maxLen > 0 ? maxLen : 524288;
    if (e.value !== t) { e.value = t; var c = Math.min(caret, t.length); try { e.setSelectionRange(c, c); } catch (x) {} }
  },
  BzWI_SetValue: function (id, textPtr) {
    if (BzWI.openId !== id || !BzWI.el) return;
    var t = UTF8ToString(textPtr);
    if (BzWI.el.value !== t) { BzWI.el.value = t; try { BzWI.el.setSelectionRange(t.length, t.length); } catch (x) {} }
  },
  BzWI_Hide: function () { BzWI.close(false); },
  BzWI_IsOpen: function () { return BzWI.openId >= 0 ? 1 : 0; },
};
autoAddDeps(BzWebInputLib, '$BzWI');
mergeInto(LibraryManager.library, BzWebInputLib);
