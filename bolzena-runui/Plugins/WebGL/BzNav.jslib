// 웹(WebGL) — 같은 탭에서 다른 주소로 옮긴다(프론티어 로그인: /api/auth/<공급자>/start → 공급자 → 돌아오면 게임이 다시 뜬다).
// 유니티 Application.OpenURL 은 웹에서 새 창(팝업 막힘)이라 쿠키 로그인 흐름에 맞지 않는다. 짝: Runtime/Kit/Online.cs 의 WebNav.
var BzNavLib = {
  BzNavigate: function (url) {
    window.location.assign(UTF8ToString(url));
  }
};
mergeInto(LibraryManager.library, BzNavLib);
