# 문체 · 용어 검사 (Docs/문체.md) — 금지 낱말 · 어미 위반을 찾는다
#
#   python style_check.py [--stage2] [--only 엔진|데이터|화면] [--show N]
#     엔진: 엔진이 짓는 글(StyleDump.exe 의 json — style.cmd 가 만든다) — 효과 글은 명사형 · 개조식
#     데이터: bolzena-content-v2/world (--stage2 면 heroes 도) — 지문 합니다체 · 단추 「~기」 · 이름 명사
#     화면: 코드 속 한글 문자열(bolzena-core Run · runui Runtime, --stage2 면 bolzena-unity) — 안내 합니다체 · 단추 명사
#   덜어 내기: 줄 끝에 // 문체:허용  (코드) · ALLOW 표(아래)
#   끝 코드: 위반 0 이면 0, 있으면 1
import json, glob, re, sys, io, os, argparse
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

ap = argparse.ArgumentParser()
ap.add_argument('--stage2', action='store_true', help='사도 JSON · 본 게임까지')
ap.add_argument('--only', default=None)
ap.add_argument('--show', type=int, default=400)
ap.add_argument('--dump', default=os.path.join(os.environ.get('TEMP', '.'), 'bz_style_all.json'))
ap.add_argument('--content', default=r'C:\projects\bolzena-content-v2')
a = ap.parse_args()

HANGUL = re.compile('[가-힣]')
# ── 용어집(Docs/문체.md §2) — 쓰지 않을 낱말 → 쓸 낱말 ─────────────────────
BANNED = [
    (r'보호막|방어막', '실드'),
    (r'싸움', '전투'),
    (r'체력', 'HP'),
    (r'코스트|(?<![\dA-Za-z])\d코(?![가-힣])', '비용'),
    (r'궁극기|필살기|고학년 스킬', '고학년'),
    (r'그로기', '격파(강인도 0)'),
    (r'행동력', 'AP'),
    (r'약점 피해', '약점 공격'),
    (r'이로운 효과', '버프'), (r'해로운 효과', '디버프'),
    (r'아군 전체|아군 전원', '파티 · 사도마다'),
    (r'대상 적', '그 적 · 적 1명'),
    (r'적 하나(?!로)', '적 1명'),
    (r'한 턴에 한 번', '턴당 1회'),
    (r'(?<![가-힣])런(?![가-힣])|이번 판|판이 끝|새 판|(?<![가-힣])판을 (?!엎)|(?<![가-힣])판마다|(?<![가-힣])판의 ', '모험'),
    (r'치유(?!사)', '회복'),
    (r"'[^']*[가-힣][^']*'", '「 」(이름) · " "(대사)'),
]
# 효과 글(명사형) — 「~다」 로 끝나는 서술. 「~마다」 · 「~다른」 등은 빼고
FX_END = re.compile(r'(?<=[가-힣])(?<![니마])(?<!합)다(?=$|[\s.,·)」—:(])')
FX_WHITE = re.compile(r'(바다|과다|먹이다)$')
# 안내 글(합니다체) — 해라체 끝
UI_END = re.compile(r'(?<=[가-힣])(?<![니마])다(?=$|[.!?…)」,]|\s*[·—]|\s*$)')
NOT_END = re.compile(r'(보다,|다른|다음|다시|다만|다섯|다양|다툼|다녀|보다 |보다$|바다|과다)')

problems = []
def add(area, where, kind, text, why):
    problems.append((area, where, kind, text, why))

def bare(s):
    """대사(큰따옴표 · 「」 대사) · 서식 태그 · 보간을 덜어 낸 글."""
    s = re.sub(r'<[^>]+>', '', s)
    s = re.sub(r'\{[^{}]*\}', '0', s)
    s = re.sub(r'"[^"]*"|“[^”]*”|\\"[^"\\]*\\"', ' ', s)
    return s

# 용어 검사에서 빼는 것 — 고유 이름(장비 · 카드 이름의 「치유」 「체력」), 몸의 힘을 뜻하는 「체력」
ALLOW = {'치유의 펜던트', '치유의 호롱불', '체력증진', '기초 체력부터',
         # 사도 카드 · 신탁 · 패시브의 굳은 이름 · 말장난(Docs/문체.md §2 예외)
         '자연 치유', '당근 치유', '현장 체력', '베개 싸움', '싸움 구경', '필살기 연습', '흉내 낸 필살기', '판을 뒤집는 두 수',
         '타이다', '교주의 천벌 - 타이다', '약초 수다', '참다 참다', '건드렸겠다?', '씨 없는 멜론 없다'}
# 판 · 놀이판을 뜻하는 「판」(모험 아님)
PAN_OK = re.compile(r'판을 (슬쩍|뒤집|엎)')
def terms(area, where, kind, text):
    if text in ALLOW: return
    b = PAN_OK.sub('', bare(text))
    if kind == 'N': b = re.sub(r'「[^」]*」', '「」', b)   # 지문 속 「」 = 이름 · 인용
    for pat, use in BANNED:
        m = re.search(pat, b)
        if m: add(area, where, kind, text, f'용어 「{m.group(0)}」 → 「{use}」')

def ends_haera(s):
    for m in UI_END.finditer(s):
        w = s[max(0, m.start() - 2):m.end() + 1]
        if NOT_END.search(s[max(0, m.start() - 1):m.end() + 2]): continue
        return w
    return None

# ── 엔진이 짓는 글 ───────────────────────────────────────────────
def check_engine():
    if not os.path.exists(a.dump):
        print(f'(엔진 글 덤프가 없다: {a.dump} — style.cmd 로 돌려라)'); return
    for x in json.load(open(a.dump, encoding='utf-8-sig')):
        src, t = x['src'], x['text']
        if src.startswith('heroes/') and not a.stage2: continue
        terms('엔진', src, 'fx', t)
        for line in re.split(r'\n|(?<=\.) ', bare(t)):
            for m in FX_END.finditer(line):
                word = re.findall(r'[가-힣]+다$', line[:m.end()])
                if word and FX_WHITE.search(word[0]): continue
                add('엔진', src, 'fx', t, f'효과 글이 「~다」 로 끝남: …{line[max(0, m.start() - 8):m.end()]}')
                break
            if '니다' in line: add('엔진', src, 'fx', t, '효과 글에 합니다체')

# ── 데이터 ────────────────────────────────────────────────────────
N_KEYS = {'scene', 'blurb', 'passSay', 'failSay'}
SAY_N = {'options', 'gamble', 'judge', 'fail', 'pass'}
E_PARENT = {'intents', 'do', 'next', 'open', 'act'}
def cat(keys):
    k = keys[-1]; par = keys[-2] if len(keys) > 1 else ''
    if k in N_KEYS: return 'N'
    if k == 'say' and par in SAY_N: return 'N'
    if k == 'say' and par in E_PARENT: return 'E'
    if k in ('label', 'leave'): return 'L'
    if k == 'name': return 'E'
    if k == 'desc' and par == 'counters': return 'D'
    if k == 'desc' and par in ('keyword', 'keywords', 'forms'): return 'D'
    if k in ('line', 'sub'): return 'D'
    return None   # 대사(phase.say) · 값(id · type …)은 보지 않는다

def check_data():
    roots = [os.path.join(a.content, 'world')] + ([os.path.join(a.content, 'heroes')] if a.stage2 else [])
    for root in roots:
        for f in sorted(glob.glob(root + '/**/*.json', recursive=True)):
            rel = os.path.relpath(f, a.content).replace(os.sep, '/')
            def walk(o, ptr, keys):
                if isinstance(o, dict):
                    for k, v in o.items(): walk(v, ptr + '/' + k, keys + [k])
                elif isinstance(o, list):
                    for i, v in enumerate(o): walk(v, ptr + '/' + str(i), keys)
                elif isinstance(o, str) and HANGUL.search(o):
                    c = cat(keys)
                    if c is None: return
                    where = rel + ptr
                    if o in ALLOW: return
                    if c != 'E' or not o.strip().endswith('!'): terms('데이터', where, c, o)
                    b = bare(o)
                    if c == 'N':
                        w = ends_haera(re.sub(r'「[^」]*」', '「」', b))
                        if w: add('데이터', where, c, o, f'지문은 합니다체: …{w}')
                    elif c in ('L', 'E', 'D'):
                        if o.strip().endswith('!') or o.startswith('「') or o.startswith('"'): return   # 외침 · 대사
                        w = ends_haera(b)
                        if w: add('데이터', where, c, o, f'{"단추" if c == "L" else "이름 · 부제"}는 명사형: …{w}')
                        if c == 'L' and '니다' in b: add('데이터', where, c, o, '단추는 「~기」 명사형')
            walk(json.load(open(f, encoding='utf-8')), '', [])
    # 사도 도감 「이야기」(runui 화면 표 roster.json — 웹판에서 뽑은 것)
    roster = [r'C:\projects\bolzena-runui-test\Assets\Resources\RunUI\roster.json'] + ([r'C:\projects\bolzena-unity\Assets\Resources\RunUI\roster.json'] if a.stage2 else [])
    for f in roster:
        if not os.path.exists(f): continue
        proj = f.split(os.sep)[2]
        for i, h in enumerate(json.load(open(f, encoding='utf-8'))['heroes']):
            o = h.get('blurb') or ''
            if not o: continue
            where = f'{proj}/roster.json/heroes/{i}/blurb'
            terms('데이터', where, 'N', o)
            w = ends_haera(re.sub(r'「[^」]*」', '「」', bare(o)))
            if w: add('데이터', where, 'N', o, f'지문은 합니다체: …{w}')

# ── 코드 속 화면 글 ───────────────────────────────────────────────
PROJ = r'C:\projects'
CODE_ROOTS = [r'C:\projects\bolzena-core\Runtime\Run', r'C:\projects\bolzena-runui\Runtime']
CODE_ROOTS2 = [r'C:\projects\bolzena-unity\Assets\Bolzena\Scripts']
SKIP_LINE = re.compile(r'Debug\.Log|Exception\(|문체:허용|^\s*//|^\s*///|^\s*\*|\[Test|Assert\.')
LIT = re.compile(r'\$?@?"((?:[^"\\]|\\.)*)"')
BUTTON = re.compile(r'(Btn\.Make|Confirm|W\.Option|NavyPill|Pill)\(')

def check_code():
    roots = CODE_ROOTS + (CODE_ROOTS2 if a.stage2 else [])
    for root in roots:
        for f in sorted(glob.glob(root + '/**/*.cs', recursive=True)):
            if os.sep + 'Demo' + os.sep in f or os.sep + 'Editor' + os.sep in f: continue
            for i, line in enumerate(open(f, encoding='utf-8-sig'), 1):
                if SKIP_LINE.search(line): continue
                code = line.split(' // ')[0]
                for m in LIT.finditer(code):
                    s = m.group(1)
                    if not HANGUL.search(s): continue
                    where = f'{os.path.relpath(f, PROJ)}:{i}'
                    terms('화면', where, 'ui', s)
                    b = bare(s.replace('\\n', '\n'))
                    w = ends_haera(b)
                    if w: add('화면', where, 'ui', s, f'안내 글은 합니다체(효과 글이면 명사형): …{w}')
                # 단추 이름 — 「~합니다」 로 끝나면 명사로
                bm = BUTTON.search(code)
                if bm:
                    for m in LIT.finditer(code[bm.end():]):
                        s = bare(m.group(1)).strip()
                        if re.fullmatch(r'[^.·—]{1,16}니다', s) and not re.search(r'[.·—]', s):
                            add('화면', f'{os.path.relpath(f, PROJ)}:{i}', 'button', s, '단추 이름은 명사로 짧게')
                        break

areas = {'엔진': check_engine, '데이터': check_data, '화면': check_code}
for name, fn in areas.items():
    if a.only and a.only != name: continue
    fn()

from collections import Counter
by = Counter(p[0] for p in problems)
print(f'문체 검사 — {"2단계 포함" if a.stage2 else "1단계 범위"} · 걸린 것 {len(problems)} ({", ".join(f"{k} {v}" for k, v in by.items()) or "없음"})')
for p in problems[:a.show]:
    print(f'[{p[0]}] {p[1]} ({p[2]}) {p[4]}\n      {p[3][:160]}')
sys.exit(1 if problems else 0)
