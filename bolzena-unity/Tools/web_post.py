# 웹 빌드 뒤처리 — WebBuild/ 를 Cloudflare Pages 에 그대로 올릴 수 있게.
#   1) 파일 하나 25MB 한도 — Build/ 의 큰 파일(.data · .wasm 등)을 20MiB 조각(name.part00 …)으로 나누고 원본을 지운다.
#      index.html 의 fetch 덧씌우기가 조각을 차례로 받아 한 줄기로 이어 유니티 로더에 넘긴다(로더 · 캐시는 원래대로).
#   2) index.html — 화면 꽉 채운 캔버스 · 로딩 막대 · 비공식 표기 · 첫 클릭 안내.
#   3) 크기 · 파일 수 점검(25MB 넘는 파일이 남으면 실패).
import json, os, sys, time, glob

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "WebBuild")
ROOT = os.path.normpath(ROOT)
LIMIT = 25 * 1000 * 1000
CHUNK = 20 * 1024 * 1024

build = os.path.join(ROOT, "Build")
if not os.path.isdir(build):
    sys.exit("WebBuild/Build 가 없다 — 먼저 빌드")

loader = [f for f in os.listdir(build) if f.endswith(".loader.js")]
if len(loader) != 1:
    sys.exit("로더를 못 찾았다: %r" % loader)
name = loader[0][: -len(".loader.js")]


def find(kind):
    c = [f for f in os.listdir(build) if f.startswith(name + "." + kind) and ".part" not in f]
    if not c:
        c = sorted({f.split(".part")[0] for f in os.listdir(build) if f.startswith(name + "." + kind + ".") and ".part" in f})
    if len(c) != 1:
        sys.exit("%s 파일을 못 찾았다: %r" % (kind, c))
    return c[0]


files = {k: find(k) for k in ("data", "framework", "wasm")}

# ── 1) 조각내기 ──
split = {}
for f in sorted(os.listdir(build)):
    p = os.path.join(build, f)
    if ".part" in f or os.path.getsize(p) <= LIMIT:
        continue
    size = os.path.getsize(p)
    parts = []
    with open(p, "rb") as fh:
        i = 0
        while True:
            b = fh.read(CHUNK)
            if not b:
                break
            pn = "%s.part%02d" % (f, i)
            with open(os.path.join(build, pn), "wb") as o:
                o.write(b)
            parts.append("Build/" + pn)
            i += 1
    os.remove(p)
    split["Build/" + f] = {"parts": parts, "size": size}
# 이미 조각난 것(다시 돌릴 때)
for f in sorted(os.listdir(build)):
    if ".part00" in f:
        base = f.split(".part")[0]
        key = "Build/" + base
        if key in split:
            continue
        parts = sorted("Build/" + x for x in os.listdir(build) if x.startswith(base + ".part"))
        split[key] = {"parts": parts, "size": sum(os.path.getsize(os.path.join(ROOT, x)) for x in parts)}

stamp = time.strftime("%Y-%m-%d %H:%M")
version = time.strftime("%Y%m%d%H%M")

# ── 2) index.html ──
LEGAL = ("비공식 팬 게임 · 비영리 — 트릭컬 리바이브의 그림 · 음성 · 설정의 저작권은 EPID Games 에 있습니다. "
         "공식과 무관하며, 권리자의 요청이 있으면 즉시 내립니다.")

html = r"""<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no">
<title>볼제나 — 트릭컬 팬 게임</title>
<link rel="icon" href="data:,">
<meta name="description" content="%(legal)s">
<style>
  :root { --bg:#05060c; --ink:#f3ead2; --sub:#a9a39a; --gold:#e8c56a; --bar:#1a2140; }
  html, body { margin:0; height:100%%; background:var(--bg); overflow:hidden; color:var(--ink);
    font-family: "Noto Sans KR", "Apple SD Gothic Neo", "Malgun Gothic", sans-serif; }
  #unity-canvas { position:fixed; inset:0; width:100%%; height:100%%; display:block; background:var(--bg); outline:none; touch-action:none; }
  #loading { position:fixed; inset:0; display:flex; flex-direction:column; align-items:center; justify-content:center; gap:18px;
    background: radial-gradient(ellipse at center, #141a33 0%%, var(--bg) 70%%); padding:16px; box-sizing:border-box; text-align:center; }
  #loading h1 { margin:0; font-size:clamp(28px, 6vw, 52px); color:var(--gold); letter-spacing:.08em; }
  #loading .tag { color:var(--sub); font-size:14px; margin-top:-10px; }
  #bar { width:min(520px, 86vw); height:12px; border-radius:6px; background:var(--bar); overflow:hidden; box-shadow: inset 0 0 0 1px #2c3560; }
  #fill { height:100%%; width:0%%; background:linear-gradient(90deg, #b8913e, var(--gold)); transition:width .15s; }
  #status { font-size:14px; color:var(--ink); min-height:1.4em; }
  #note { font-size:13px; color:var(--sub); max-width:min(640px, 92vw); line-height:1.6; }
  #legal { position:fixed; left:0; right:0; bottom:0; padding:8px 16px; font-size:12px; color:var(--sub); text-align:center;
    background:linear-gradient(transparent, rgba(0,0,0,.6)); pointer-events:none; }
  #legal b { color:var(--gold); font-weight:600; }
  #error { color:#ff8a7a; font-size:14px; white-space:pre-wrap; max-width:min(640px, 92vw); }
</style>
</head>
<body>
<canvas id="unity-canvas" tabindex="-1"></canvas>
<div id="loading">
  <h1>볼제나</h1>
  <div class="tag">트릭컬 리바이브 팬 덱빌딩 로그라이크</div>
  <div id="bar"><div id="fill"></div></div>
  <div id="status">불러오는 중…</div>
  <div id="note">처음 한 번은 %(total_mb)sMB 를 내려받습니다(다음부터는 브라우저에 저장된 것을 씁니다).<br>
    PC 의 크롬 · 엣지 · 파이어폭스를 권합니다. 소리는 화면을 한 번 누른 뒤부터 납니다.</div>
  <div id="error"></div>
</div>
<div id="legal"><b>비공식 팬 게임 · 비영리</b> — 트릭컬 리바이브의 그림 · 음성 · 설정의 저작권은 EPID Games 에 있습니다. 공식과 무관하며, 권리자의 요청이 있으면 즉시 내립니다. · 빌드 %(stamp)s</div>
<script>
// 25MB 넘는 파일은 조각으로 올렸다 — 로더가 원래 이름을 부르면 조각을 차례로 받아 한 줄기로 잇는다
const SPLIT = %(split)s;
const VERSION = "%(version)s";
const LAST_MODIFIED = new Date(%(mtime)d).toUTCString(), ETAG = '"' + VERSION + '"';
const realFetch = window.fetch.bind(window);
let bigDone = 0, bigTotal = 0;
for (const k in SPLIT) bigTotal += SPLIT[k].size;
window.fetch = function (input, init) {
  const url = typeof input === "string" ? input : input.url;
  for (const k in SPLIT) {
    if (!url.split("?")[0].endsWith(k)) continue;
    // 로더 캐시(IndexedDB) 재검증 — 이 판의 조각이면 304(내려받지 않음), HEAD 는 머리만
    const h = new Headers((init && init.headers) || {});
    if (h.get("If-Modified-Since") === LAST_MODIFIED || h.get("If-None-Match") === ETAG) return Promise.resolve(new Response(null, { status: 304 }));
    if (init && init.method === "HEAD") return Promise.resolve(new Response(null, { status: 200, headers: headersOf(k) }));
    return joined(k);
  }
  return realFetch(input, init);
};
function joined(k) {
  const s = SPLIT[k];
  let i = 0, reader = null;
  const body = new ReadableStream({
    async pull(ctrl) {
      for (;;) {
        if (!reader) {
          if (i >= s.parts.length) { ctrl.close(); return; }
          const r = await realFetch(s.parts[i++] + "?v=" + VERSION);
          if (!r.ok) { ctrl.error(new Error(s.parts[i - 1] + " — " + r.status)); return; }
          reader = r.body.getReader();
        }
        const { done, value } = await reader.read();
        if (done) { reader = null; continue; }
        bigDone += value.byteLength;
        ctrl.enqueue(value);
        return;
      }
    }
  });
  return Promise.resolve(new Response(body, { status: 200, headers: headersOf(k) }));
}
function headersOf(k) {
  return { "Content-Length": String(SPLIT[k].size), "Content-Type": "application/octet-stream", "Last-Modified": LAST_MODIFIED, "ETag": ETAG };
}

const fill = document.getElementById("fill"), status = document.getElementById("status"), err = document.getElementById("error");
const canvas = document.getElementById("unity-canvas");
function fit() {
  const r = window.devicePixelRatio || 1;
  canvas.width = Math.round(window.innerWidth * r);
  canvas.height = Math.round(window.innerHeight * r);
}
fit();
window.addEventListener("resize", fit);

const config = {
  arguments: [],
  dataUrl: "Build/%(data)s",
  frameworkUrl: "Build/%(framework)s",
  codeUrl: "Build/%(wasm)s",
  streamingAssetsUrl: "StreamingAssets",
  companyName: "sadodesk",
  productName: "Bolzena",
  productVersion: VERSION,
  autoSyncPersistentDataPath: true,   // 이어하기 저장(persistentDataPath)을 IndexedDB 에 바로 적는다
  matchWebGLToCanvasSize: true,
  devicePixelRatio: window.devicePixelRatio || 1,
  showBanner: (msg, type) => { if (type === "error") err.textContent += msg + "\n"; console.log("[unity " + type + "] " + msg); },
};

const script = document.createElement("script");
script.src = "Build/%(name)s.loader.js?v=" + VERSION;
script.onload = () => {
  createUnityInstance(canvas, config, (p) => {
    const shown = bigTotal > 0 ? Math.max(p, Math.min(0.9, bigDone / bigTotal * 0.9)) : p;
    fill.style.width = (shown * 100).toFixed(1) + "%%";
    status.textContent = p >= 0.9 && bigDone >= bigTotal ? "준비하는 중…" : "불러오는 중… " + Math.round(shown * 100) + "%%";
  }).then((inst) => {
    window.unityInstance = inst;
    document.getElementById("loading").style.display = "none";
    document.getElementById("legal").style.display = "none";   // 게임 로비가 같은 표기를 띄운다
    canvas.focus();
  }).catch((e) => { err.textContent += String(e) + "\n"; status.textContent = "불러오지 못했습니다"; });
};
script.onerror = () => { status.textContent = "로더를 받지 못했습니다"; };
document.body.appendChild(script);
</script>
</body>
</html>
"""

first = sum(os.path.getsize(os.path.join(build, f)) for f in os.listdir(build))
mtime = int(time.time() * 1000)
with open(os.path.join(ROOT, "index.html"), "w", encoding="utf-8") as o:
    o.write(html % {
        "legal": LEGAL, "split": json.dumps(split, ensure_ascii=False), "version": version, "stamp": stamp, "mtime": mtime,
        "data": files["data"], "framework": files["framework"], "wasm": files["wasm"], "name": name,
        "total_mb": int(round(first / 1e6)),
    })

# 쓰지 않는 템플릿 찌꺼기
for f in ("TemplateData",):
    p = os.path.join(ROOT, f)
    if os.path.isdir(p):
        import shutil
        shutil.rmtree(p)

over = []
count = 0
total = 0
largest = ("", 0)
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        p = os.path.join(dp, f)
        s = os.path.getsize(p)
        total += s
        count += 1
        if s > largest[1]:
            largest = (os.path.relpath(p, ROOT), s)
        if s > LIMIT:
            over.append((p, s))

print("[web] 조각낸 파일: " + ", ".join("%s → %d조각" % (k, len(v["parts"])) for k, v in split.items()))
print("[web] 첫 로딩(Build/) %.1fMB · 전체 %.1fMB · 파일 %d개 · 가장 큰 파일 %s %.1fMB" % (first / 1e6, total / 1e6, count, largest[0], largest[1] / 1e6))
if over:
    sys.exit("[web] 25MB 넘는 파일: %r" % over)
if count > 20000:
    sys.exit("[web] 파일이 2만 개를 넘는다")
