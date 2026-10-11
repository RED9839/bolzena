# 사도 갈래(2026-10-11 엘다인 20명 전환) — 원작 에셋 복사기(이 폴더 copy_assets.py · bolzena-unity Tools/copy_run_assets.py)가
#   사도마다 무엇을 복사할지 정한다. 콘텐츠(bolzena-content-v2)를 읽어 사도 표(roster.json)의 그림 키(art)마다 갈래 하나:
#     P = 플레이어 사도(heroes) — 전부(스파인 · 목소리 · 효과음 · 카드 그림 · 아이콘 · 스탠딩 · 미리 구운 그림)
#     C = 클론 전용 사도(world/clones 의 cloneHeroes — 보스 클론) — SD · 스탠딩 스파인 · 초상 · 스탠딩 렌더 · 고학년 목소리(등장 · 고학년 컷인)만
#     N = 이벤트 NPC 로만 나오는 사도(events 의 npc) — SD · 스탠딩 스파인 · 초상 · 스탠딩 렌더만
#     없음(표에만 있는 사도) — 복사하지 않는다
#   콘텐츠를 못 읽으면 모두 P(예전처럼 135명 전부). 사도를 되살리면(백업 → heroes) 저절로 P 가 된다.
import glob, json, os

CONTENT = r"C:\projects\bolzena-content-v2"


def norm(s):
    return (s or "").replace(" ", "")


def _link(roster, name):
    # runui Roster.Link 와 같은 짝 — 이름이 같거나(공백 무시) 표 이름이 코어 이름으로 시작하면
    n = norm(name)
    return next((h for h in roster if norm(h.get("ko")) == n or norm(h.get("key")) == n), None) \
        or next((h for h in roster if norm(h.get("key")).startswith(n) or norm(h.get("ko")).startswith(n)), None)


def kinds(roster, content=CONTENT):
    """그림 키 → "P" · "C" · "N". 표에 있어도 여기 없으면 복사하지 않는 사도."""
    if not os.path.isdir(os.path.join(content, "heroes")):
        return {h["art"]: "P" for h in roster if h.get("art")}
    out = {}

    def put(name, k):
        h = _link(roster, name)
        if h and h.get("art") and h["art"] not in out:
            out[h["art"]] = k
    for f in sorted(glob.glob(os.path.join(content, "heroes", "**", "*.json"), recursive=True)):
        for h in json.load(open(f, encoding="utf-8")).get("heroes", []):
            put(h["name"], "P")
    for f in sorted(glob.glob(os.path.join(content, "world", "clones", "*.json"))):
        for h in json.load(open(f, encoding="utf-8")).get("cloneHeroes", []):
            put(h["name"], "C")
    for f in sorted(glob.glob(os.path.join(content, "world", "events", "*.json"))):
        if os.path.basename(f).startswith("_"):
            continue
        for e in json.load(open(f, encoding="utf-8")).get("events", []):
            if e.get("npc"):
                h = next((x for x in roster if x.get("ko") == e["npc"]), None)   # 판 화면(Flow.Event)이 이름으로 찾는다
                if h and h.get("art") and h["art"] not in out:
                    out[h["art"]] = "N"
    return out


def card_keep(roster, kind_of):
    """카드 id → 그 카드 그림을 둘지 — 주인(가장 긴 사도 id 앞머리)이 플레이어 사도가 아니면 뺀다. 사도 id 가 안 붙은 카드(샘플 · 교주)는 둔다."""
    owners = sorted(((h["key"], kind_of.get(h.get("art"))) for h in roster if h.get("key")), key=lambda x: -len(x[0]))

    def keep(cid):
        for key, k in owners:
            if cid.startswith(key + "_"):
                return k == "P"
        return True
    return keep
