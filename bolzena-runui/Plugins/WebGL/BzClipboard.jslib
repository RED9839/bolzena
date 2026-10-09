// 웹(WebGL) 클립보드 — 유니티 GUIUtility.systemCopyBuffer 는 브라우저 클립보드에 닿지 않는다(진행 코드 복사가 비어 있던 까닭).
// 복사: navigator.clipboard.writeText → 안 되면 숨긴 textarea + document.execCommand('copy') → 그래도 안 되면 다음 터치 · 클릭(진짜 사용자 이벤트)
//   안에서 한 번 더(execCommand). 결과는 유니티 오브젝트 「BzClipboard」 의 OnCopied("1" | "0")로 돌려준다.
// 붙여넣기: navigator.clipboard.readText(https · 권한 · 브라우저에 따라 막힌다) → OnPasted(글 | "" = 못 읽음). 막히면 화면의 코드 입력 칸에 붙여 넣는다.
// 짝: Runtime/Kit/WebClipboard.cs.
var BzClipboardLib = {
  $BzClip: {
    host: 'BzClipboard',
    pending: null,
    send: function (method, msg) {
      var f = (typeof Module !== 'undefined' && Module.SendMessage) || (window.unityInstance && window.unityInstance.SendMessage && window.unityInstance.SendMessage.bind(window.unityInstance));
      if (f) f(BzClip.host, method, msg);
    },
    execCopy: function (text) {
      var ok = false;
      var ta = document.createElement('textarea');
      ta.value = text;
      ta.setAttribute('readonly', '');
      var s = ta.style;
      s.position = 'fixed'; s.left = '0'; s.top = '0'; s.width = '1px'; s.height = '1px'; s.opacity = '0'; s.fontSize = '16px';   // 16px — iOS 가 확대하지 않게
      document.body.appendChild(ta);
      try {
        ta.focus(); ta.select(); ta.setSelectionRange(0, text.length);   // iOS 는 setSelectionRange 가 있어야 고른다
        ok = document.execCommand('copy');
      } catch (e) { ok = false; }
      document.body.removeChild(ta);
      return !!ok;
    },
    // 다음 사용자 이벤트(터치 끝 · 클릭) 안에서 한 번 더 — 유니티는 단추를 다음 프레임에 처리해서 사용자 활성화가 지났을 수 있다
    armRetry: function (text) {
      BzClip.pending = text;
      var once = function () {
        ['pointerup', 'touchend', 'click', 'keydown'].forEach(function (t) { document.removeEventListener(t, once, true); });
        var t = BzClip.pending; BzClip.pending = null;
        if (t == null) return;
        BzClip.send('OnCopied', BzClip.execCopy(t) ? '1' : '0');
      };
      ['pointerup', 'touchend', 'click', 'keydown'].forEach(function (t) { document.addEventListener(t, once, true); });
    },
    copy: function (text) {
      var fallback = function () {
        if (BzClip.execCopy(text)) BzClip.send('OnCopied', '1');
        else { BzClip.armRetry(text); BzClip.send('OnCopied', 'retry'); }
      };
      try {
        if (navigator.clipboard && navigator.clipboard.writeText && window.isSecureContext) {
          navigator.clipboard.writeText(text).then(function () { BzClip.send('OnCopied', '1'); }, fallback);
          return;
        }
      } catch (e) { }
      fallback();
    },
  },

  BzClip_Copy: function (textPtr) {
    BzClip.copy(UTF8ToString(textPtr));
  },

  BzClip_Paste: function () {
    try {
      if (navigator.clipboard && navigator.clipboard.readText && window.isSecureContext) {
        navigator.clipboard.readText().then(function (t) { BzClip.send('OnPasted', t || ''); }, function () { BzClip.send('OnPasted', ''); });
        return;
      }
    } catch (e) { }
    BzClip.send('OnPasted', '');
  },
};

autoAddDeps(BzClipboardLib, '$BzClip');
mergeInto(LibraryManager.library, BzClipboardLib);
