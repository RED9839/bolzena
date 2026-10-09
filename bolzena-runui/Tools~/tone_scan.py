# 화면 글 어미 분포 — 시스템 문구가 합쇼체(~합니다 · ~하세요)로 모였는지 본다
#
#   python "Tools~/tone_scan.py"              분포 + 어긋난 줄(해요 · 해라 · 음슴) 목록
#   python "Tools~/tone_scan.py" --all        분포 + 갈래마다 보기 몇 줄
#   python "Tools~/tone_scan.py" --area 코드   코드만(runui Runtime · unity Scripts) / 데이터만(content-v2 world 이벤트 지문)
#
# 규칙(2026-10-09):
#   안내 · 설명 문장 · 알림(토스트)  → 합쇼체(~합니다 · ~하세요)
#   단추 · 칩 · 제목                → 명사형 · 짧은 동사(「모험 시작」 · 「고르기」)
#   효과 글(카드 · 적의 수 풀이)     → 명사형 · 개조식(「~씀」 「~획득」 그대로 — 이 검사에서는 「효과」 로 따로 센다)
#   사도 대사(hero_lines · 이벤트 따옴표 대사 · 클론 보스 대사)는 사도 말투 그대로 — 따옴표 안은 보지 않는다
# 덜어 내기: 코드 줄 끝 // 문체:허용 · 줄에 Debug.Log · Exception · Demo · Editor 폴더
# 해라체(~다)의 자세한 검사와 용어 검사는 bolzena-core/Tools~/TextCheck/Style/style_check.py 가 맡는다.
import argparse, glob, io, json, os, re, sys
from collections import Counter, defaultdict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
ap = argparse.ArgumentParser()
ap.add_argument('--all', action='store_true')
ap.add_argument('--area', default=None, choices=[None, '코드', '데이터'])
ap.add_argument('--show', type=int, default=80)
a = ap.parse_args()

ROOT = r'C:\projects'
CODE = [r'C:\projects\bolzena-runui\Runtime', r'C:\projects\bolzena-unity\Assets\Bolzena\Scripts']
DATA = r'C:\projects\bolzena-content-v2\world\events'
HANGUL = re.compile('[가-힣]')
LIT = re.compile(r'\$?@?"((?:[^"\\]|\\.)*)"')
SKIP = re.compile(r'Debug\.Log|Exception\(|문체:허용|^\s*//|^\s*\*|\[Test|Assert\.|Expect\(|Bail\(|LogWarning|LogError')
# 효과 · 적의 수 풀이 · 카드 글을 짓는 곳 — 명사형이 규칙이라 「효과」 로 센다
FX_FILES = re.compile(r'CardTerms|CardText|Widgets\.cs$|TermPop|Intent|Keyword|Status|^Terms\.cs$|^Look\.cs$', re.I)
# 사도 · NPC 대사만 든 곳(말투 그대로 둔다)과 개발용 표(화면에 안 나옴) — 통째로 뺀다
SPEECH_FILES = re.compile(r'^(HeroLines|UltVideoFit|UltCutin|EnemyLayout)\.cs$')
SPEECH_BLOCK = re.compile(r'string\[\]\s+\w*Lines\b')          # 대사 배열 선언 — 「};」 까지 뺀다
SPEECH_LINE = re.compile(r'\bW\.Bubble\(|\bSay\(|HeroLines\.')     # 대사 한 줄

HAPSYO = re.compile(r'(니다|니까|세요|십시오|시오|봅시다|합시다|갑시다)$')
HAEYO = re.compile(r'(?<!세)(?<!시)요$')
HAERA = re.compile(r'(?<![니마])(?<!합)다$')
EUMSEUM = re.compile(r'(함|됨|없음|있음|않음|받음|남음|씀|짐|았음|었음|였음|했음|됐음|줌|봄|냄|얻음|잃음|오름|내림|모름|끝남|바뀜|사라짐|늘어남|줄어듦|듦|나감|들어감)$')
NOT_HAERA = re.compile(r'(다른|다음|다시|다만|바다|과다|타이다|보다)$')


def bare(s):
    s = s.replace('\\n', '\n')
    s = re.sub(r'<[^>]+>', '', s)
    s = re.sub(r'\{[^{}]*\}', '0', s)
    s = re.sub(r'\\"[^"\\]*\\"|"[^"]*"|“[^”]*”', ' ', s)   # 따옴표 대사
    return s


def chunks(s):
    for part in re.split(r'\n|(?<=[.!?…])\s+| — | · |\s*\|\s*', s):
        p = re.sub(r'\([^()]*\)?$|\([^()]*\)', '', part)   # 괄호 풀이는 떼고 본문 끝을 본다
        p = re.sub(r'[\s.!?…·,)」』\]~]+$', '', p).strip()
        if HANGUL.search(p): yield p


def kind(p):
    w = re.findall(r'[가-힣]+$', p)
    if not w: return '기타'
    w = w[0]
    if HAPSYO.search(w): return '합쇼'
    if HAEYO.search(w): return '해요'
    if HAERA.search(w) and not NOT_HAERA.search(w): return '해라'
    if EUMSEUM.search(w): return '음슴'
    return '명사'


counts = defaultdict(Counter)
samples = defaultdict(list)


def note(area, where, s, fx=False):
    for p in chunks(bare(s)):
        k = kind(p)
        if fx and k in ('음슴', '명사'): k = '효과'
        counts[area][k] += 1
        samples[(area, k)].append((where, p, s))


if a.area in (None, '코드'):
    for root in CODE:
        for f in sorted(glob.glob(root + '/**/*.cs', recursive=True)):
            if re.search(r'[\\/](Demo|Editor)[\\/]', f) or SPEECH_FILES.search(os.path.basename(f)): continue
            fx = bool(FX_FILES.search(os.path.basename(f)))
            speech = False
            for i, line in enumerate(open(f, encoding='utf-8-sig'), 1):
                if SPEECH_BLOCK.search(line): speech = True
                if speech:
                    if '};' in line: speech = False
                    continue
                if SPEECH_LINE.search(line): continue
                if SKIP.search(line): continue
                code = line.split(' // ')[0]
                for m in LIT.finditer(code):
                    if HANGUL.search(m.group(1)):
                        note('코드', f'{os.path.relpath(f, ROOT)}:{i}', m.group(1), fx)

if a.area in (None, '데이터'):
    # 이벤트 지문(scene · say · passSay · failSay) — 따옴표 대사는 bare 가 덜어 낸다. 단추(label)는 명사형이라 따로 센다
    for f in sorted(glob.glob(DATA + '/*.json')):
        if os.path.basename(f).startswith('_'): continue
        rel = os.path.relpath(f, ROOT)

        def walk(o, ptr, key):
            if isinstance(o, dict):
                for k, v in o.items(): walk(v, f'{ptr}/{k}', k)
            elif isinstance(o, list):
                for i, v in enumerate(o): walk(v, f'{ptr}/{i}', key)
            elif isinstance(o, str) and HANGUL.search(o):
                if key in ('scene', 'say', 'passSay', 'failSay', 'blurb'): note('데이터', rel + ptr, re.sub(r'「[^」]*」', '「」', o))   # 지문 속 「」 = 이름 · 쪽지 · 간판 글
                elif key in ('label', 'leave'): note('데이터·단추', rel + ptr, o)
        walk(json.load(open(f, encoding='utf-8')), '', '')

ORDER = ['합쇼', '명사', '효과', '해요', '음슴', '해라', '기타']
for area, c in counts.items():
    tot = sum(c.values())
    print(f'── {area}: 문장 조각 {tot}')
    print('   ' + ' · '.join(f'{k} {c[k]} ({c[k] * 100 // max(1, tot)}%)' for k in ORDER if c[k]))

bad = ['해요', '해라'] + (['음슴'] if True else [])
print('\n── 어긋난 것(해요 · 해라 · 안내 글의 음슴) — 단추 · 칩 · 제목이면 그대로 둔다')
n = 0
for area in counts:
    if area == '데이터·단추': continue
    for k in bad:
        for where, p, s in samples[(area, k)]:
            if n >= a.show: break
            print(f'[{k}] {where}\n      …{p[-40:]}')
            n += 1
if a.all:
    for (area, k), xs in samples.items():
        print(f'\n── 보기 {area} · {k}')
        for where, p, s in xs[:6]: print(f'   {where}  …{p[-40:]}')
