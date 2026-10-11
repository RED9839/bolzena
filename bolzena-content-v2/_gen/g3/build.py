# -*- coding: utf-8 -*-
"""장비 · 교주 카드 리뉴얼(2026-10-10 셋째) — 카제나 키워드 빌드의 「파츠」 로.

    python _gen/g3/build.py [출력 데이터 폴더]   (기본: 이 저장소 C:/projects/bolzena-content-v2)

src/ 의 리뉴얼 전 데이터(2026-10-10 16:20 백업과 같음)를 읽어
  · 장비: 이름 · 등급 · 칸 · 능력치 · 아이콘(id)은 그대로, 효과(effect)와 설명(blurb)만 EQ 표대로 갈아 끼운다.
  · 교주 카드: 이름 · 등급 · 값 · id 는 그대로, CARD 표의 손질을 한 장씩 적용한다.
  · 모든 「limit」(턴당 · 전투당 횟수 제한)을 지운다(2026-10-09~10 사용자 결정 — 횟수 제한 없음).
결과를 world/equips/{무기,방어구,장신구}.json · world/neutral/교주카드.json 에 쓴다.
문서: _measure/장비_스펠_리뉴얼/
"""
import copy, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'src')
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.normpath(os.path.join(HERE, '..', '..'))

# ── 효과 조각 ───────────────────────────────────────────────────────────
def st(i, v=1, t=None):
    f = {'k': 'status', 'id': i, 'v': v}
    if t: f['target'] = t
    return f
def dmg(r, t='oneEnemy', hits=None, base=None, fixed=False, ofEvent=None):
    f = {'k': 'dmg', 'ratio': r, 'target': t}
    if hits: f['hits'] = hits
    if base: f['base'] = base
    if fixed: f['fixed'] = True
    if ofEvent: f['ofEvent'] = ofEvent
    return f
def extra(r, t='oneEnemy'): return {'k': 'extra', 'ratio': r, 'target': t}
def shield(r, fixed=False):
    f = {'k': 'shield', 'ratio': r}
    if fixed: f['fixed'] = True
    return f
def heal(r): return {'k': 'heal', 'ratio': r}
def draw(v=1): return {'k': 'draw', 'v': v}
def ap(v=1): return {'k': 'ap', 'v': v}
def nextap(v): return {'k': 'nextAp', 'v': v}
def cheaper(v=1): return {'k': 'nextCheaper', 'v': v}
def tough(v=1, t=None):
    f = {'k': 'tough', 'v': v}
    if t: f['target'] = t
    return f
def empower(r, who=None):
    f = {'k': 'empower', 'ratio': r}
    if who: f['who'] = who
    return f
def mod(k, v, t='self', run=False):
    f = {'k': k, 'v': v, 'target': t}
    if run: f['run'] = True
    return f
def gauge(v): return {'k': 'gauge', 'v': v}
def cleanse(v=1): return {'k': 'cleanse', 'v': v}
def burn(v=1): return {'k': 'burn', 'v': v}
def discard(v=1): return {'k': 'discard', 'v': v}
def strip(t='oneEnemy'): return {'k': 'strip', 'target': t}
def pull(frm='discard', n=None, unique=False, tag=None, to=None):
    f = {'k': 'pull', 'from': frm}
    if unique: f['unique'] = True
    if tag: f['tag'] = tag
    if n: f['n'] = n
    if to: f['to'] = to
    return f
def when(on): return {'k': 'when', 'on': on}
def power(i, *rules): return {'k': 'power', 'id': i, 'rules': list(rules)}

def rule(on, fx, name=None, conds=None, **w):
    r = {}
    if name: r['name'] = name
    r['when'] = dict({'on': on}, **w)
    if conds: r['conds'] = conds
    r['fx'] = fx
    return r

# ── 장비 ────────────────────────────────────────────────────────────────
# id → (효과 규칙 목록, 설명 · None 이면 그대로). 표에 없는 장비는 효과 없이 능력치만(일반 「스탯만」).
ANY = {'who': 'any'}
EQ = {
    # 무기 ─ 전설
    'eq_kettlebell': ([rule('play', [st('잔광', 1)], '한 번에 들어 올리기', minCost=2, who='any', every=2)],
                      '느리지만 한 번 휘두르면 묵직합니다. 무거운 카드를 낼 때마다 다음 한 수에 힘이 실립니다.'),
    'eq_furnace': ([rule('turnStart', [mod('atkMod', 0.04, run=True)], '달아오르는 날'),
                    rule('play', [mod('atkMod', 0.04, run=True)], '옮겨붙는 불', tag='열정', who='any')],
                   '싸울수록 달아오르는 검. 뜨거운 카드가 지나가면 더 달아오릅니다.'),
    'eq_rustyred': ([rule('always', [mod('dealtMod', 0.2)], '벼랑 끝', conds=[{'c': 'wounded'}]),
                     rule('lowHp', [st('사기', 2)], '붉은 오기', pct=0.5)],
                    '반쯤 다치면 붉게 빛납니다. 알고 보니 말라붙은 석류 주스.'),
    'eq_glove': ([rule('kill', [st('사기', 1)], '기세', mine=True),
                  rule('kill', [draw(1)], '다음 상대')],
                 '한 놈 쓰러뜨릴 때마다 주먹이 빨라지고, 다음 상대가 눈에 들어옵니다.'),
    # 무기 ─ 희귀
    'eq_sharpstaff': ([rule('play', [st('잔광', 1)], '지팡이 끝 칼날', type='스킬', every=2)],
                      '지팡이와 검을 하나로 합친 물건. 주문을 외우면 칼날이 다음 공격을 벼립니다.'),
    'eq_mace': ([rule('hurt', [st('칼날 벼리기', 1)], '웃으며 참회')],
                '맞을수록 손이 매워지는 메이스. 아프게 맞은 만큼 방패로 되갚습니다.'),
    'eq_cloakdagger': ([rule('play', [empower(0.25)], '망토 속 단검', maxCost=0, who='any')],
                       '괴도 놀이 소품. 가벼운 손놀림이 지나간 자리에 단검이 숨어 있습니다.'),
    # 무기 ─ 고급
    'eq_elfwand': ([rule('fightStart', [shield(1.0)], '반짝 실드')], None),
    'eq_steelsword': ([rule('play', [tough(1)], '무게 싣기', type='공격', minCost=2)], None),
    'eq_burnbranch': ([rule('exhaust', [st('그을림', 1, 'randomEnemy')], '타오르는 재', who='any')],
                      '불이 붙은 나뭇가지. 다 쓴 카드를 태우면 불똥이 적에게 튑니다.'),
    'eq_obsidianstar': ([rule('play', [extra(0.4, 'randomEnemy')], '곁따라 던지기', type='공격', maxCost=1, every=2)], None),
    'eq_mistdagger': ([rule('discard', [extra(0.25, 'randomEnemy')], '안개 속 한 수', who='any')],
                      '안개처럼 흐릿한 날의 비수. 손에서 흩어진 카드 뒤에서 날이 나옵니다.'),
    'eq_oldarrow': ([rule('crit', [st('약점 노출', 1, 'oneEnemy')], '곧게 날기')],
                    '오래됐지만 아직 곧게 납니다. 제대로 꽂힌 화살이 급소를 짚어 줍니다.'),
    'eq_foxdagger': ([rule('always', [mod('dealtMod', 0.06)], '여우 손놀림')], None),
    'eq_ouchsword': ([rule('play', [st('균열', 2, 'oneEnemy')], '쓰라림', type='공격', every=3)], None),
    'eq_taser': ([rule('play', [st('충격', 1, 'oneEnemy')], '찌릿', type='공격', every=3)], None),
    'eq_firebow': ([rule('play', [st('그을림', 2, 'oneEnemy')], '불화살', tag='열정', who='any')],
                   '옆에만 둬도 후끈한 불화살 활. 뜨거운 카드에 불이 옮겨붙습니다.'),
    # 방어구 ─ 전설
    'eq_squishyvest': ([rule('fightStart', [shield(0.8)], '말랑 완충'),
                        rule('lowHp', [st('피해 감소', 2)], '눌린 자국', pct=0.5)],
                       '무엇이든 말랑하게 받아 내는 조끼. 많이 눌릴수록 더 말랑하게 받아 냅니다.'),
    'eq_growpotion': ([rule('fightStart', [shield(1.2)], '쑥쑥'),
                       rule('always', [mod('dealtMod', 0.1)], '큰 주먹')], None),
    # 방어구 ─ 희귀
    'eq_goblinhat': ([rule('lowHp', [st('절대 무적', 1)], '사라지기', pct=0.15)], None),
    'eq_blessedpauldron': ([rule('turnEnd', [heal(0.35)], '굳은 축복', conds=[{'c': 'guarded', 'not': True}])],
                           '어깨에 얹은 축복. 막 없이 버틴 날이면 쉬지 않고 상처를 돌봅니다.'),
    'eq_safetybelt': ([rule('fightStart', [shield(1.5), st('실드 유지', 1)], '비상 실드')], None),
    'eq_scalearmor': ([rule('blocked', [st('반격', 1)], '비늘 세우기')],
                      '단단한 비늘을 엮은 갑옷. 다 막아 내면 비늘이 곤두섭니다.'),
    'eq_balloon': ([rule('lowHp', [shield(2.5)], '예비 풍선', pct=0.5)], None),
    'eq_rubbergauntlet': ([rule('play', [shield(0.8)], '튕겨 돌아오기', tag='회수', who='any')],
                          '고무처럼 늘어나는 건틀릿. 손으로 돌아오는 카드를 낼 때마다 튕겨 막아 줍니다.'),
    # 방어구 ─ 고급
    'eq_madmask': ([rule('hurt', [{'k': 'reflect', 'ratio': 0.2}], '섬뜩한 웃음', guarded=True)], None),
    'eq_thorncrown': ([rule('hurt', [st('피해 감소', 1)], '가시의 각오', pct=0.1)], None),
    'eq_leather': ([rule('turnEnd', [heal(0.4)], '질긴 가죽', conds=[{'c': 'wounded'}])], None),
    # 방어구 ─ 일반
    'eq_cardboard': ([rule('fightStart', [shield(0.8)], '한 겹 더')],
                     '택배 상자를 겹겹이 댄 갑옷. 전투가 시작되면 상자 한 겹이 먼저 받쳐 줍니다.'),
    # 장신구 ─ 전설
    'eq_sunnycard': ([rule('play', [extra(1.5, 'randomEnemy')], '맑은 날 번개', type='공격', every=3)], None),
    'eq_eldainlamp': ([rule('fightStart', [mod('healMod', 0.4, run=True)], '따스한 심지'),
                       rule('overheal', [dmg(0.5, 'randomEnemy', fixed=True, ofEvent=0.5)], '넘치는 불빛', who='any')],
                      '엘다인의 숲을 비추던 램프. 넘치는 온기는 불빛이 되어 적을 태웁니다.'),
    'eq_lifegem': ([rule('lowHp', [heal(1.5)], '마지막 맥박', pct=0.3)], None),
    'eq_assassinbook': ([rule('play', [st('회피', 1)], '숨죽이기', type='공격', every=4),
                         rule('play', [extra(0.6)], '그림자 찌르기', type='공격', maxCost=0)], None),
    'eq_toyscope': ([rule('play', [st('약점 노출', 1, 'oneEnemy')], '멀리서 조준', type='공격', every=3)], None),
    'eq_volcanijuice': ([rule('turnStart', [shield(0.2)], '과즙 코팅')],
                        '화산섬 과일을 짠 주스. 몸을 감싼 막이 날마다 새로 돋습니다.'),
    'eq_battery': ([rule('play', [extra(2.0, 'randomEnemy')], '충전 완료', type='스킬', every=3)], None),
    # 장신구 ─ 희귀
    'eq_grail': ([rule('spend', [draw(1)], '차오르는 기원', who='any')], None),
    'eq_jadebook': ([rule('fightStart', [st('다음 턴 드로우', 1)], '펼쳐 둔 장')],
                    '전투 전에 다음 주문이 적힌 장을 펼쳐 둡니다.'),
    'eq_goldcrown': ([rule('ult', [st('사기', 1)], '왕의 한 수')], None),
    'eq_combatguide': ([rule('fightStart', [st('약점 노출', 1, 'randomEnemy')], '급소 복습')], None),
    'eq_healpendant': ([rule('play', [heal(0.4)], '피어나는 빛', type='공격')], None),
    'eq_censer': ([rule('shieldBreak', [shield(0.5)], '향 피우기')],
                  '향 연기가 감싼 막은 깨져도 다시 피어오릅니다.'),
    'eq_muffin': ([rule('exhaust', [strip(), extra(0.4)], '펑', who='any')], None),
    # 장신구 ─ 고급
    'eq_luckydice': ([rule('crit', [heal(0.5)], '좋은 눈')], None),
    'eq_tiara': ([rule('fightStart', [st('결정화', 1)], '빛나는 막')],
                 '머리 위에서 빛나며 턴이 끝날 때마다 얇은 막을 한 겹 두릅니다.'),
    'eq_diabracelet': ([rule('play', [shield(0.8), draw(1)], '겹쳐 박은 다이아', tag='결속', who='any')],
                       '다이아 조각을 겹겹이 박은 팔찌. 조각이 겹친 카드를 낼 때마다 반짝 막이 섭니다.'),
    'eq_greedring': ([rule('fightStart', [st('고통 각인', 1, 'allEnemies')], '더 아프게')], None),
    'eq_worshipframe': ([rule('play', [heal(0.6), st('다음 턴 드로우', 1)], '마지막 장면', tag='종극', who='any')],
                        '멋진 장면을 넣어 둘 액자. 하루를 마치는 장면에서 모두 기운을 차립니다.'),
    'eq_cutletpin': ([rule('play', [draw(1), cheaper(1)], '배꼽시계', tag='주도', who='any')],
                     '보기만 해도 배고파지는 머리핀. 먼저 달려드는 손에 한 장이 더 붙고, 다음 손이 가벼워집니다.'),
    # 장신구 ─ 일반
    'eq_gemring': ([rule('play', [empower(0.15)], '주문 뒤 한 방', type='스킬')],
                   '주문을 쓰고 나면 보석이 반짝여 다음 한 수를 거듭니다.'),
    'eq_healinglamp': ([rule('fightStart', [mod('healMod', 0.15, run=True)], '작은 불빛')], None),
    'eq_catpaw': ([rule('fightStart', [empower(0.5)], '고양이 기운')],
                  '고양이 발 모양 부적. 첫 수에 기운을 싣습니다.'),
}

# ── 교주 카드 ───────────────────────────────────────────────────────────
def O(name, fx, tags=None, cost=None, pw=False):
    o = {'name': name}
    if cost is not None: o['cost'] = cost
    if tags: o['tags'] = tags
    o['fx'] = fx
    if pw: o['power'] = True
    return o
def B(name, kind=None, fx=None, tags=None):
    b = {'name': name}
    if kind: b['kind'] = kind
    if fx: b['fx'] = fx
    if tags: b['tags'] = tags
    return b

def keep_tags(c, *add, drop=()):
    """기본 · 신탁 모두에 태그를 더한다(신탁 태그는 기본을 통째로 갈아 끼우므로 신탁마다 넣는다)."""
    def fix(lst):
        lst = [t for t in (lst or []) if t.split(' ')[0] not in drop]
        for a in add:
            if a not in lst: lst.append(a)
        return lst
    c['tags'] = fix(c.get('tags'))
    for o in c.get('oracles', []):
        o['tags'] = fix(o.get('tags'))

CARD = {}
def card(cid):
    def deco(fn): CARD[cid] = fn; return fn
    return deco

# 일반 ────────────────────────────────────────────────────────────────
@card('n_apprentice')   # 견습 마법사 — 어느 덱에나: 손 돌리기 · 버리기(탄환 · 연결 · 공명의 연료)
def _(c):
    c['oracles'][3] = O('마법 노트', [st('공명', 2), draw(2), power('마법 노트', rule('turnStart', [st('공명', 1)]))], pw=True)

@card('n_strawberry')   # 딸기맛 캡슐 — 어느 덱에나: 회복(쥐여 주기 +5%p 넘어 회복량을 낮춤)
def _(c):
    c['fx'] = [heal(0.6), cleanse(1)]
    o = c['oracles']
    o[0] = O('두 알 삼키기', [heal(1.0), cleanse(1)], tags=['소멸'])
    o[1] = O('아이 달래기', [heal(0.6), cleanse(1), draw(1)], tags=['소멸'])
    o[2] = O('쓴맛 뒤 단맛', [heal(0.6), cleanse(1)], tags=['소멸 2'])
    o[3] = O('사탕인 척', [heal(0.6), cleanse(1), st('면역', 1)], tags=['소멸', '보존'])
    o[4] = O('한 통 다', [heal(0.9), cleanse(2), st('다음 턴 드로우', 1)], tags=['소멸'])

@card('n_soda')   # 소다맛 캡슐 — 어느 덱에나: AP
def _(c): pass

@card('n_afterimage')   # 그건 내 잔상 — 어느 덱에나. 회피는 쥐여 주기 +9~16%p 라 피해 감소로(회피는 신탁1 에만)
def _(c):
    c['tags'] = ['소멸']
    c['fx'] = [st('피해 감소', 2), draw(1)]
    c['oracles'] = [
        O('반 박자 빠르게', [st('회피', 1), draw(2)], tags=['소멸']),
        O('비켜서기', [st('피해 감소', 2), draw(1), shield(0.8)], tags=['소멸']),
        O('뒤로 돌아가기', [st('피해 감소', 2), draw(1), st('둔화', 1, 'allEnemies')], tags=['소멸']),
        O('잔상 남기기', [st('피해 감소', 2), draw(1), st('다음 턴 드로우', 1)], tags=['소멸']),
        O('잔상', [st('피해 감소', 2)], tags=['소멸'], cost=0),
    ]
    c['blurb'] = '맞은 줄 알았는데 맞은 건 잔상이었습니다. 그 틈에 손이 바뀝니다.'

@card('n_healthy')
def _(c): pass

@card('n_selfdev')
def _(c): pass

@card('n_stamina')
def _(c): pass

@card('n_coin')   # 랜덤 코인 — 탄환 빌드: 효과로 버리면 탄환이 튄다, 내면 드로우 · 동전 던지기
def _(c):
    keep_tags(c, '탄환')
    c['blurb'] = '얼마나 나올지 두근두근. 손에서 튕겨 내면 동전이 총알처럼 날아갑니다.'

# 고급 ────────────────────────────────────────────────────────────────
@card('n_nat_mad')   # 성격의 카드[광기] — 열정 빌드
def _(c):
    keep_tags(c, '열정')
    c['oracles'][2] = O('폭주 직전', [dmg(1.0), st('고통', 3, 'oneEnemy'), when('passion'), st('고통', 5, 'randomEnemy')], tags=['열정'])
    c['blurb'] = '광기가 하나 더 있는 셈 칩니다. 다른 뜨거운 카드가 지나가면 덩달아 들끓습니다.'

@card('n_nat_pure')   # 성격의 카드[순수] — 축복 빌드
def _(c):
    keep_tags(c, '축복')
    c['blurb'] = '순수가 하나 더 있는 셈 칩니다. 내려놓기만 해도 맑은 막이 됩니다.'

@card('n_nat_cool')   # 성격의 카드[냉정] — 어디나. 적 전체 약화가 쥐여 주기 +5%p → 적 1명
def _(c):
    def fix(fx):
        for f in fx:
            if f.get('k') == 'status' and f.get('id') == '약화' and f.get('target') == 'allEnemies':
                f['target'] = 'oneEnemy'
    fix(c['fx'])
    for o in c['oracles']: fix(o['fx'])

@card('n_nat_lively')   # 성격의 카드[활발] — 주도 빌드
def _(c):
    c['tags'] = ['주도']
    c['fx'] = [draw(2), st('협공', 1)]
    c['oracles'] = [
        O('펄쩍', [draw(3), st('협공', 1)], tags=['주도']),
        O('다 같이 놀자', [draw(2), st('협공', 2)], tags=['주도']),
        O('쉬지 않고', [dmg(1.2), draw(2), st('협공', 1)], tags=['주도']),
        O('먼저 뛰기', [draw(2), st('협공', 1), power('먼저 뛰기', rule('play', [st('협공', 1)], tag='주도', who='any'))], pw=True),
        O('깡총', [draw(2)], tags=['주도'], cost=0),
    ]
    c['blurb'] = '활발이 하나 더 있는 셈 칩니다. 맨 먼저 내밀면 몸이 가볍습니다.'

@card('n_nat_gloomy')   # 성격의 카드[우울] — 소각 빌드
def _(c):
    c['fx'] = [burn(1), shield(1.2), st('실드 유지', 1)]
    c['oracles'] = [
        O('이불 속', [burn(1), shield(1.2), st('결정화', 2)]),
        O('깊은 우울', [burn(1), shield(1.8), st('실드 유지', 1)]),
        O('혼잣말', [burn(1), shield(1.2), draw(2)]),
        O('그늘', [shield(1.2), power('그늘', rule('exhaust', [st('결정화', 1)], who='any'))], pw=True),
        O('작은 한숨', [burn(1), shield(1.2)], cost=0),
    ]
    c['blurb'] = '우울이 하나 더 있는 셈 칩니다. 내려놓은 만큼 웅크립니다.'

@card('n_bulletproof')
def _(c): pass

@card('n_scholar')
def _(c): pass

@card('n_bully')
def _(c): pass

@card('n_strongman')   # 차력 시범 — 천상 빌드: 비용 2 이상 카드를 내면 손에서 저절로
def _(c):
    c['cost'] = 1
    c['tags'] = ['천상']
    c['fx'] = [dmg(1.1), tough(1)]
    c['oracles'] = [
        O('기왓장 열 장', [dmg(1.1), tough(2)], tags=['천상']),
        O('쇠사슬 끊기', [dmg(0.9, 'allEnemies'), tough(1, 'allEnemies')], tags=['천상']),
        O('관객 호응', [dmg(1.1), tough(1), draw(1)], tags=['천상']),
        O('맨손 격파', [dmg(3.2), tough(2)], cost=2),
        O('앙코르', [dmg(1.0), tough(1)], tags=['천상'], cost=0),
    ]
    c['blurb'] = '무거운 걸 들어 올리면 박수가 터지고, 박수가 나오면 한 번 더 합니다.'

@card('n_critical')   # 회심의 일격 — 회수 빌드: 내도 손으로 돌아오는 한 방
def _(c):
    c['tags'] = ['회수 3']
    c['fx'] = [dmg(0.9), st('충격', 2, 'oneEnemy')]
    c['oracles'] = [
        O('번쩍', [dmg(0.9), st('충격', 3, 'oneEnemy')], tags=['회수 3']),
        O('틈새', [dmg(1.3), st('충격', 2, 'oneEnemy')], tags=['회수 3']),
        O('호흡 고르기', [dmg(0.9), st('충격', 2, 'oneEnemy'), draw(1)], tags=['회수 3']),
        O('모두에게 한 번씩', [dmg(0.9, 'allEnemies'), st('충격', 2, 'allEnemies')], tags=['회수 3']),
        O('노리고 기다리기', [dmg(0.8), st('충격', 2, 'oneEnemy')], tags=['회수 3'], cost=0),
    ]
    c['blurb'] = '노린 데로 정확히. 빗나가도 다시 손에 쥐고 노립니다.'

@card('n_training')
def _(c): pass

@card('n_promote')
def _(c): pass

@card('n_leave')   # 조기 퇴근 — 종극 빌드(그대로)
def _(c): pass

# 희귀 ────────────────────────────────────────────────────────────────
@card('n_catchhim')   # 저놈 잡아라! — 회수 빌드 · 약점 만들기
def _(c):
    keep_tags(c, '회수 3')
    c['fx'] = [dmg(0.9), st('약점 노출', 1, 'oneEnemy')]
    o = c['oracles']
    o[0] = O('현상금', [dmg(0.9), st('약점 노출', 2, 'oneEnemy')], tags=['회수 3'])
    o[1] = O('막다른 골목', [dmg(1.2), st('약점 노출', 1, 'oneEnemy'), {'k': 'ifKill'}, ap(1)], tags=['회수 3'])
    o[2] = O('다 같이 쫓아가', [dmg(0.45, hits=3), st('약점 노출', 1, 'oneEnemy')], tags=['회수 3'])
    o[3] = O('놓치지 마', [st('약점 노출', 1, 'oneEnemy'), {'k': 'perDebuff'}, dmg(0.7)], tags=['회수 4'])
    o[4] = O('저기다!', [dmg(0.8), st('약점 노출', 1, 'oneEnemy')], tags=['회수 3'], cost=0)
    c['blurb'] = '목표가 생기니 다 같이 쫓아갑니다. 놓쳐도 몇 번이고 다시 쫓아갑니다.'

@card('n_morale')
def _(c): pass

@card('n_vanguard')
def _(c):
    c['oracles'][1] = O('굳은 대열', [shield(1.86), power('굳은 대열', rule('turnStart', [st('반격', 1)]))], pw=True)

@card('n_center')   # 중앙GOOD — 소각 빌드
def _(c):
    c['oracles'][2] = O('균형', [draw(2), power('균형', rule('exhaust', [st('다음 턴 드로우', 1)], who='any'))], pw=True)

@card('n_rearguard')   # 후발대 — 어느 덱에나(고유 카드 덱 서치) + 회수 · 결속 갈래
def _(c):
    o = c['oracles']
    o[2] = O('신호탄', [pull('discard', tag='회수', n=2)], tags=['소멸'])
    o[4] = O('한 발만', [pull('discard', tag='결속'), draw(1)], tags=['소멸'])

@card('n_safehit')   # 안 아프게 맞는 법 — 불굴 1(전투 내내)이 쥐여 주기 +11%p → 피해 감소(횟수형)
def _(c):
    c['fx'] = [shield(1.4), st('피해 감소', 2)]
    o = c['oracles']
    o[0] = O('굴러서 받기', [shield(1.4), st('피해 감소', 2), st('회피', 1)])
    o[1] = O('맷집', [shield(2.0), st('피해 감소', 1), power('맷집', rule('shieldBreak', [st('피해 감소', 1)]))], pw=True)
    o[2] = O('낙법', [shield(2.1), st('피해 감소', 2)])
    o[3] = O('예습', [shield(1.4), st('피해 감소', 2), draw(1)], tags=['개전'])
    o[4] = O('반만 맞기', [shield(1.3), st('피해 감소', 1)], cost=0)

@card('n_warmheart')   # 따뜻한 마음씨 — 축복 빌드
def _(c):
    keep_tags(c, '축복')
    o = c['oracles'][2]; o['fx'][-1]['rules'][0]['fx'][0]['ratio'] = 0.7
    c['blurb'] = '마음이 따뜻하면 상처도 빨리 아뭅니다. 꺼내지 못한 마음도 막이 됩니다.'

@card('n_recovery')   # 효율적인 회복 — 쥐여 주기 +8%p → 회복량을 낮춤 · 초재생 뺌
def _(c):
    c['fx'] = [cheaper(1), heal(0.4)]
    o = c['oracles']
    o[0] = O('계획된 휴식', [cheaper(1), heal(0.9)], tags=['소멸'])
    o[1] = O('회복 일지', [cheaper(1), heal(0.4), draw(1)], tags=['소멸'])
    o[3] = O('미리 챙기기', [cheaper(1), heal(0.4), st('다음 턴 드로우', 1)], tags=['개전', '소멸'])
    o[4] = O('몰아서 쉬기', [cheaper(2), heal(0.6), nextap(-1)], tags=['소멸'])

@card('n_sprint')   # 막판 스퍼트 — 열정 빌드
def _(c):
    c['oracles'][2] = O('두 번째 숨', [draw(1), power('두 번째 숨', rule('lowHp', [ap(1), draw(2)], pct=0.5))], pw=True)
    keep_tags(c, '열정')
    c['blurb'] = '몰릴수록 다리가 빨라집니다. 옆에서 뜨겁게 뛰면 덩달아 뜁니다.'

@card('n_membership')   # 멤버십 카드 — 주도 빌드(그대로)
def _(c): pass

@card('n_openrun')   # 오픈런 — 개막 빌드. 0코 개막 드로우 2 는 쥐여 주기 +13%p → 1코(첫 턴 AP 를 치른다)
def _(c):
    c['cost'] = 1
    c['tags'] = ['개막', '소멸']
    c['fx'] = [draw(2), st('다음 턴 드로우', 1)]
    c['oracles'] = [
        O('새벽 줄서기', [draw(3), st('다음 턴 드로우', 1)], tags=['개막', '소멸']),
        O('번호표', [draw(2), st('다음 턴 드로우', 2)], tags=['개막', '소멸']),
        O('문 열자마자', [draw(2), st('다음 턴 드로우', 1), st('사기', 1)], tags=['개막', '소멸']),
        O('한정판', [draw(1), st('저장', 1)], tags=['개막', '소멸'], cost=0),
        O('친구랑 같이', [draw(2), st('다음 턴 드로우', 1), st('협공', 1)], tags=['개막', '소멸']),
    ]
    c['blurb'] = '문 열리자마자 저절로 달려듭니다. 줄 선 값은 첫 차례의 손으로 치릅니다.'

@card('n_snap')   # 이성의 끈 — 개막 빌드(그대로)
def _(c): pass

# 전설 ────────────────────────────────────────────────────────────────
@card('n_tactics')
def _(c):
    c['oracles'][1] = O('작전 지도', [st('사기', 2), power('작전 지도', rule('turnStart', [st('약점 노출', 1, 'randomEnemy')]))])

@card('n_master')   # 전투의 달인 — 스킬을 낼수록 다음 카드가 세진다(자동 추가타 대신)
def _(c):
    R = lambda r: rule('play', [empower(r, 'any')], type='스킬', who='any')
    c['fx'] = [st('사기', 1), power('달인의 눈', R(0.15))]
    o = c['oracles']
    o[0] = O('기술 전수', [st('사기', 1), power('달인의 눈', R(0.25))])
    o[1] = O('달인의 시범', [st('사기', 1), power('달인의 눈', rule('play', [empower(0.25, 'any')], type='공격', who='any'))])
    o[2] = O('몸에 밴 기술', [st('사기', 1), st('계몽', 1), power('달인의 눈', R(0.15))])
    o[3] = O('가벼운 몸풀기', [st('사기', 1), power('달인의 눈', R(0.15))], cost=0)
    o[4] = O('비전 전수', [st('사기', 2), power('달인의 눈', R(0.3))], cost=2)
    c['blurb'] = '기술을 쓸 때마다 다음 수가 더 매서워집니다.'

@card('n_charm')   # 치명적인 매력 — 약점 만들기. 쥐여 주기 +6.7%p → 협공 1 줄임
def _(c):
    for fx in [c['fx']] + [o['fx'] for o in c['oracles']]:
        for f in fx:
            if f.get('k') == 'status' and f.get('id') == '협공' and f['v'] > 1: f['v'] -= 1

@card('n_skipgrade')   # 단체 월반 — 천상 빌드(비용 2)
def _(c): pass

@card('n_potion')   # 수상한 물약 — 개막. 쥐여 주기 +13%p → 고통 5 · 각인 → 고통 3
def _(c):
    c['fx'] = [dmg(0.4, 'allEnemies'), st('고통', 2, 'allEnemies')]
    o = c['oracles']
    o[0] = O('진한 색', [dmg(0.4, 'allEnemies'), st('고통', 4, 'allEnemies')], tags=['개막', '소멸'])
    o[1] = O('뚜껑을 열면', [dmg(0.4, 'allEnemies'), st('고통', 2, 'allEnemies'), st('취약', 1, 'allEnemies')], tags=['개막', '소멸'])
    o[2] = O('부작용 없음', [dmg(0.4, 'allEnemies'), st('고통', 2, 'allEnemies')], tags=['개막'])
    o[3] = O('독 안개', [st('고통', 5, 'allEnemies'), st('고통 각인', 1, 'allEnemies')], tags=['개막', '소멸'])
    o[4] = O('작은 병', [dmg(0.3, 'allEnemies'), st('고통', 2, 'allEnemies')], tags=['개막', '소멸'], cost=0)

@card('n_slipin')
def _(c): pass

@card('n_aroma')
def _(c): pass

@card('n_meditate')   # 명상의 시간 — 종극 빌드: AP 를 남기고(종극으로 일찍) 턴을 마치면 다음 손이 넉넉해진다
def _(c):
    K = lambda n=1: power('고른 숨', rule('keepAp', [st('다음 턴 드로우', n)]))
    c['fx'] = [st('집중', 1), K()]
    c['oracles'] = [
        O('깊은 명상', [st('집중', 2), K()]),
        O('비운 마음', [st('집중', 1), K()], cost=0),
        O('호흡 세기', [st('집중', 1), power('호흡 세기', rule('play', [st('저장', 1), st('다음 턴 드로우', 1)], tag='종극', who='any'))]),
        O('아침 명상', [st('집중', 1), st('다음 턴 드로우', 1), K()], tags=['개전']),
        O('긴 명상', [st('집중', 2), K(2)], cost=2),
    ]
    c['blurb'] = '숨을 고르며 일찍 손을 내려놓으면 다음 손이 넉넉합니다.'

@card('n_picnic')   # 파스텔빛 소풍 — 천상 빌드(비용 2)
def _(c): pass

@card('n_surprise')   # 깜짝 상자 — 개막. 전투마다 기절이 쥐여 주기 +44%p → 기절 대신 둔화
def _(c):
    c['fx'] = [dmg(0.4, 'allEnemies'), st('둔화', 1, 'allEnemies')]
    o = c['oracles']
    o[0] = O('두 겹 상자', [dmg(0.7, 'allEnemies'), st('둔화', 1, 'allEnemies')], tags=['개막', '소멸'])
    o[1] = O('리본을 풀면', [dmg(0.4, 'allEnemies'), st('둔화', 1, 'allEnemies')], tags=['개막'])
    o[2] = O('놀란 틈', [dmg(0.4, 'allEnemies'), st('둔화', 1, 'allEnemies'), st('취약', 1, 'allEnemies')], tags=['개막', '소멸'])
    o[3] = O('상자 속 상자', [dmg(0.4, 'allEnemies'), st('둔화', 2, 'allEnemies')], tags=['개막', '소멸'])
    o[4] = O('작은 상자', [dmg(0.3, 'allEnemies'), st('둔화', 1, 'allEnemies')], tags=['개막', '소멸'], cost=0)
    c['blurb'] = '안에 뭐가 들었는지는 맞은 녀석만 압니다. 놀라서 한 박자 늦습니다.'


# ── 만들기 ──────────────────────────────────────────────────────────────
def strip_limits(x):
    if isinstance(x, dict):
        x.pop('limit', None)
        for v in x.values(): strip_limits(v)
    elif isinstance(x, list):
        for v in x: strip_limits(v)

def load(name): return json.load(open(os.path.join(SRC, name), encoding='utf-8'))
def save(path, obj):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = path + '.tmp'
    with open(tmp, 'w', encoding='utf-8', newline='\n') as f:
        f.write(json.dumps(obj, ensure_ascii=False, indent=2) + '\n')
    os.replace(tmp, path)

def main():
    seen = set()
    for slot in ('무기', '방어구', '장신구'):
        d = load(slot + '.json')
        for e in d['equips']:
            seen.add(e['id'])
            if e['id'] in EQ:
                eff, blurb = EQ[e['id']]
                e['effect'] = copy.deepcopy(eff)
                if blurb: e['blurb'] = blurb
            else:
                e.pop('effect', None)
            strip_limits(e)
        save(os.path.join(OUT, 'world', 'equips', slot + '.json'), d)
    miss = set(EQ) - seen
    if miss: raise SystemExit(f'EQ 표에 없는 장비 id: {miss}')
    d = load('교주카드.json')
    ids = {c['id'] for c in d['cards']}
    miss = set(CARD) - ids
    if miss: raise SystemExit(f'CARD 표에 없는 카드 id: {miss}')
    for c in d['cards']:
        if c['id'] in CARD: CARD[c['id']](c)
        strip_limits(c)
    save(os.path.join(OUT, 'world', 'neutral', '교주카드.json'), d)
    print('장비', len(seen), '· 교주 카드', len(d['cards']), '→', OUT)

if __name__ == '__main__':
    main()
