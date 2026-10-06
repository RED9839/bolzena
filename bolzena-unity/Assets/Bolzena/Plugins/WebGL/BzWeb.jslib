// 웹 빌드 ↔ index.html(Tools/web_post.py 가 쓴다) — 번들 받는 동안 로딩 화면을 다시 띄우고 진행 · 실패를 적는다(WebBundles.cs).
mergeInto(LibraryManager.library, {
  BzWebStatus: function (p, msg) {
    var s = UTF8ToString(msg);
    if (typeof window !== "undefined" && window.bzStatus) window.bzStatus(p, s);
  },
  // index.html(web_post.py)이 번들(WebBuild/Bundles/manifest.json)이 있는 판인지 적어 둔다 — 없으면 목록을 묻지도 않는다
  BzWebHasBundles: function () {
    return (typeof window !== "undefined" && window.bzBundles) ? 1 : 0;
  },
  BzWebReady: function () {
    if (typeof window !== "undefined" && window.bzReady) window.bzReady();
  }
});
